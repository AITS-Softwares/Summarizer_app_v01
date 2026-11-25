using System.Net.Http.Headers;
using System.Text.Json;
using Summarizer.Api.Contracts;
using Summarizer.Api.Models;

namespace Summarizer.Api.Services;

public sealed record AzureOcrOutcome(string? Text, string? ConfigurationMessage);

/// <summary>Uses Azure Document Intelligence Layout to OCR PDF pages without rendering them locally.</summary>
public sealed class AzureDocumentIntelligenceService(
    IHttpClientFactory httpClientFactory,
    AiProviderService aiProviderService,
    ILogger<AzureDocumentIntelligenceService> logger)
{
    private const string ApiVersion = "2024-11-30";

    public static string NormalizeEndpoint(string? value) => value?.Trim().TrimEnd('/') ?? string.Empty;

    public async Task<AzureOcrOutcome> ExtractAsync(string filePath, ProviderSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.AzureDocumentIntelligenceEndpoint) || string.IsNullOrWhiteSpace(settings.AzureDocumentIntelligenceKeyEncrypted))
            return new AzureOcrOutcome(null, "Azure OCR is selected but its endpoint or key is not configured in Administration.");

        try
        {
            var client = CreateClient(settings);
            await using var stream = File.OpenRead(filePath);
            using var request = new HttpRequestMessage(HttpMethod.Post, AnalyzeUrl(settings))
            {
                Content = new StreamContent(stream)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            using var submitted = await client.SendAsync(request, cancellationToken);
            if (!submitted.IsSuccessStatusCode)
                return new AzureOcrOutcome(null, ToMessage("Azure OCR could not accept this PDF", submitted));

            if (!submitted.Headers.TryGetValues("operation-location", out var locations) || string.IsNullOrWhiteSpace(locations.FirstOrDefault()))
                return new AzureOcrOutcome(null, "Azure OCR did not return a processing operation. Check the configured endpoint and Document Intelligence resource.");

            var resultUrl = locations.First();
            for (var attempt = 0; attempt < 60; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                using var result = await client.GetAsync(resultUrl, cancellationToken);
                if (!result.IsSuccessStatusCode) return new AzureOcrOutcome(null, ToMessage("Azure OCR status check failed", result));
                var payload = await result.Content.ReadAsStringAsync(cancellationToken);
                using var json = JsonDocument.Parse(payload);
                var status = json.RootElement.TryGetProperty("status", out var state) ? state.GetString() : null;
                if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    var text = ReadText(json.RootElement);
                    return string.IsNullOrWhiteSpace(text)
                        ? new AzureOcrOutcome(null, "Azure OCR completed but no readable text was returned.")
                        : new AzureOcrOutcome(text, null);
                }
                if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
                    return new AzureOcrOutcome(null, "Azure OCR could not read this PDF. Check the source file and Azure service logs.");
            }
            return new AzureOcrOutcome(null, "Azure OCR is taking longer than expected. Please retry the document.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Azure Document Intelligence OCR failed");
            return new AzureOcrOutcome(null, "Azure OCR could not be completed. Check the Azure endpoint, key, and service availability.");
        }
    }

    public async Task<ProviderTestResponse> TestAsync(ProviderSettings settings, CancellationToken cancellationToken)
    {
        var endpoint = NormalizeEndpoint(settings.AzureDocumentIntelligenceEndpoint);
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(settings.AzureDocumentIntelligenceKeyEncrypted))
            return new ProviderTestResponse(false, "Configure an Azure endpoint and key first.", "Azure Document Intelligence", endpoint);
        try
        {
            var client = CreateClient(settings);
            using var response = await client.GetAsync($"{endpoint}/documentintelligence/documentModels?api-version={ApiVersion}", cancellationToken);
            return new ProviderTestResponse(response.IsSuccessStatusCode,
                response.IsSuccessStatusCode ? "Azure Document Intelligence accepted the configured key." : ToMessage("Azure rejected the configured endpoint or key", response),
                "Azure Document Intelligence", endpoint);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Azure Document Intelligence connection test failed");
            return new ProviderTestResponse(false, "Azure connection test failed. Check the endpoint, network, and key.", "Azure Document Intelligence", endpoint);
        }
    }

    private HttpClient CreateClient(ProviderSettings settings)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", aiProviderService.UnprotectApiKey(settings.AzureDocumentIntelligenceKeyEncrypted!));
        return client;
    }

    private static string AnalyzeUrl(ProviderSettings settings) =>
        $"{NormalizeEndpoint(settings.AzureDocumentIntelligenceEndpoint)}/documentintelligence/documentModels/{Uri.EscapeDataString(settings.AzureDocumentIntelligenceModel)}:analyze?api-version={ApiVersion}";

    private static string? ReadText(JsonElement root)
    {
        if (!root.TryGetProperty("analyzeResult", out var result)) return null;
        if (result.TryGetProperty("pages", out var pages) && pages.ValueKind == JsonValueKind.Array)
        {
            var text = new List<string>();
            foreach (var page in pages.EnumerateArray())
            {
                var number = page.TryGetProperty("pageNumber", out var pageNumber) ? pageNumber.GetInt32() : text.Count + 1;
                text.Add($"--- Page {number} ---");
                if (page.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array)
                    text.AddRange(lines.EnumerateArray().Where(line => line.TryGetProperty("content", out _)).Select(line => line.GetProperty("content").GetString() ?? string.Empty).Where(line => !string.IsNullOrWhiteSpace(line)));
            }
            if (text.Count > 0) return string.Join(Environment.NewLine, text);
        }
        return result.TryGetProperty("content", out var content) ? content.GetString() : null;
    }

    private static string ToMessage(string prefix, HttpResponseMessage response) => $"{prefix} ({(int)response.StatusCode} {response.ReasonPhrase}).";
}
