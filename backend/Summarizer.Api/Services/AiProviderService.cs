using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Summarizer.Api.Models;
using Summarizer.Api.Prompts;

namespace Summarizer.Api.Services;

public sealed record AiResult(string Content, bool RequiresConfiguration);

public sealed class AiProviderService(
    IHttpClientFactory httpClientFactory,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<AiProviderService> logger)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("SummaryStudio.ApiKey.v1");

    public string ProtectApiKey(string apiKey) => _protector.Protect(apiKey);

    public string GetApiKeyType(ProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKeyEncrypted)) return "none";

        try
        {
            var apiKey = _protector.Unprotect(settings.ApiKeyEncrypted);
            if (apiKey.StartsWith("sk-ant-", StringComparison.OrdinalIgnoreCase)) return "anthropic";
            if (apiKey.StartsWith("sk-or-", StringComparison.OrdinalIgnoreCase)) return "openrouter";
            if (apiKey.StartsWith("gsk_", StringComparison.OrdinalIgnoreCase)) return "groq";
            if (apiKey.StartsWith("sk-", StringComparison.OrdinalIgnoreCase)) return "openai";
            return "unknown";
        }
        catch
        {
            return "unreadable";
        }
    }

    public static string NormalizeApiBaseUrl(string value)
    {
        var url = value.Trim().TrimEnd('/');
        var suffixes = new[]
        {
            "/chat/completions",
            "/messages"
        };

        foreach (var suffix in suffixes)
        {
            while (url.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                url = url[..^suffix.Length].TrimEnd('/');
            }
        }

        return url;
    }

    public static bool IsAnthropic(ProviderSettings settings) =>
        Uri.TryCreate(settings.ApiBaseUrl, UriKind.Absolute, out var uri)
        && uri.Host.EndsWith("anthropic.com", StringComparison.OrdinalIgnoreCase);

    public static string GetApiEndpoint(ProviderSettings settings) =>
        IsAnthropic(settings)
            ? $"{NormalizeApiBaseUrl(settings.ApiBaseUrl)}/messages"
            : $"{NormalizeApiBaseUrl(settings.ApiBaseUrl)}/chat/completions";

    public async Task<AiResult> AnalyzeAsync(
        ProviderSettings settings,
        string provider,
        string prompt,
        string documentContext,
        CancellationToken cancellationToken)
    {
        var credentialMismatch = GetCredentialMismatch(settings, provider);
        if (credentialMismatch is not null)
        {
            return new AiResult(credentialMismatch, true);
        }

        try
        {
            return provider.Equals("local", StringComparison.OrdinalIgnoreCase)
                ? await CallOllamaAsync(settings, prompt, documentContext, cancellationToken)
                : IsAnthropic(settings)
                    ? await CallAnthropicAsync(settings, prompt, documentContext, cancellationToken)
                    : await CallOpenAiCompatibleAsync(settings, prompt, documentContext, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "AI provider request failed for {Provider}", provider);
            return new AiResult(
                $"The {provider} provider could not complete this request. Check the provider URL, model, and credentials in Settings. Technical detail: {exception.Message}",
                true);
        }
    }

    private string? GetCredentialMismatch(ProviderSettings settings, string provider)
    {
        if (!provider.Equals("api", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(settings.ApiKeyEncrypted))
        {
            return null;
        }

        var apiKeyType = GetApiKeyType(settings);
        if (apiKeyType == "unreadable")
        {
            return "The saved API key can no longer be decrypted. Re-enter the key in Settings.";
        }

        var isAnthropicKey = apiKeyType == "anthropic";
        var isOpenRouter = Uri.TryCreate(settings.ApiBaseUrl, UriKind.Absolute, out var uri)
            && uri.Host.EndsWith("openrouter.ai", StringComparison.OrdinalIgnoreCase);

        if (isOpenRouter && isAnthropicKey)
        {
            return "A Claude Console key was entered for OpenRouter. Select the Anthropic provider to use an sk-ant- key, or enter an OpenRouter sk-or- key.";
        }

        if (IsAnthropic(settings) && !isAnthropicKey)
        {
            return "The Anthropic provider expects a Claude Console API key beginning with sk-ant-. Choose the matching provider for this key.";
        }

        return null;
    }

    private async Task<AiResult> CallOpenAiCompatibleAsync(
        ProviderSettings settings,
        string prompt,
        string documentContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKeyEncrypted))
        {
            return new AiResult(
                "The AI API is selected but no API key is configured. Open Settings, add an OpenAI-compatible API URL, model, and API key, then retry.",
                true);
        }

        var apiKey = _protector.Unprotect(settings.ApiKeyEncrypted);
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            model = settings.ApiModel,
            messages = BuildMessages(prompt, documentContext),
            temperature = 0.2
        };

        using var response = await client.PostAsync(
            GetApiEndpoint(settings),
            JsonContent.Create(body),
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseBody);

        using var json = JsonDocument.Parse(responseBody);
        var content = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
        return new AiResult(content ?? "The provider returned an empty response.", false);
    }

    private async Task<AiResult> CallAnthropicAsync(
        ProviderSettings settings,
        string prompt,
        string documentContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKeyEncrypted))
        {
            return new AiResult(
                "Anthropic is selected but no Claude API key is configured. Add a key from the Anthropic Console in Settings, then retry.",
                true);
        }

        var apiKey = _protector.Unprotect(settings.ApiKeyEncrypted);
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var body = new
        {
            model = settings.ApiModel,
            max_tokens = 2048,
            system = DocumentAnalysisPrompt.System,
            messages = new[] { new { role = "user", content = DocumentAnalysisPrompt.BuildUserContent(prompt, documentContext) } }
        };

        using var response = await client.PostAsync(
            GetApiEndpoint(settings),
            JsonContent.Create(body),
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseBody);

        using var json = JsonDocument.Parse(responseBody);
        var content = json.RootElement
            .GetProperty("content")
            .EnumerateArray()
            .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "text")
            .Select(item => item.GetProperty("text").GetString())
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
        return new AiResult(content ?? "Anthropic returned an empty response.", false);
    }

    private async Task<AiResult> CallOllamaAsync(
        ProviderSettings settings,
        string prompt,
        string documentContext,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        var body = new
        {
            model = settings.LocalModel,
            messages = BuildMessages(prompt, documentContext),
            stream = false
        };

        using var response = await client.PostAsync(
            $"{settings.LocalBaseUrl.TrimEnd('/')}/api/chat",
            JsonContent.Create(body),
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(responseBody);
        var content = json.RootElement.GetProperty("message").GetProperty("content").GetString();
        return new AiResult(content ?? "The local model returned an empty response.", false);
    }

    private static object[] BuildMessages(string prompt, string documentContext)
    {
        return
        [
            new { role = "system", content = DocumentAnalysisPrompt.System },
            new { role = "user", content = DocumentAnalysisPrompt.BuildUserContent(prompt, documentContext) }
        ];
    }

    private static void EnsureSuccess(HttpResponseMessage response, string responseBody)
    {
        if (response.IsSuccessStatusCode) return;

        var detail = responseBody.Length > 800 ? responseBody[..800] : responseBody;
        throw new HttpRequestException(
            $"Provider returned {(int)response.StatusCode} ({response.ReasonPhrase}). {detail}");
    }
}
