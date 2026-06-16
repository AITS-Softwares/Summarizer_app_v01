using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Models;
using Summarizer.Api.Services;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController(AppDbContext dbContext, AiProviderService aiProviderService) : ControllerBase
{
    [HttpGet]
    public async Task<ProviderSettingsResponse> Get(CancellationToken cancellationToken)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return ToResponse(settings);
    }

    [HttpPut]
    public async Task<ActionResult<ProviderSettingsResponse>> Update(
        UpdateProviderSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProviderMode is not ("api" or "local"))
        {
            return BadRequest("ProviderMode must be 'api' or 'local'.");
        }

        var settings = await GetSettingsAsync(cancellationToken);
        settings.ProviderMode = request.ProviderMode;
        settings.ApiBaseUrl = AiProviderService.NormalizeApiBaseUrl(request.ApiBaseUrl);
        settings.ApiModel = request.ApiModel.Trim();
        settings.LocalBaseUrl = request.LocalBaseUrl.Trim().TrimEnd('/');
        settings.LocalModel = request.LocalModel.Trim();
        settings.UpdatedAtUtc = DateTime.UtcNow;

        if (request.ClearApiKey)
        {
            settings.ApiKeyEncrypted = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            settings.ApiKeyEncrypted = aiProviderService.ProtectApiKey(request.ApiKey.Trim());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(settings));
    }

    [HttpPost("test")]
    public async Task<ActionResult<ProviderTestResponse>> Test(CancellationToken cancellationToken)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        var endpoint = settings.ProviderMode == "local"
            ? $"{settings.LocalBaseUrl.TrimEnd('/')}/api/chat"
            : AiProviderService.GetApiEndpoint(settings);
        var providerName = settings.ProviderMode == "local"
            ? "Ollama"
            : AiProviderService.IsAnthropic(settings) ? "Anthropic" : "OpenAI-compatible API";

        var result = await aiProviderService.AnalyzeAsync(
            settings,
            settings.ProviderMode,
            "Reply with exactly: Connection successful",
            string.Empty,
            cancellationToken);

        return Ok(new ProviderTestResponse(
            !result.RequiresConfiguration,
            result.RequiresConfiguration ? result.Content : "Connection successful. The provider accepted the configured key and model.",
            providerName,
            endpoint));
    }

    private async Task<ProviderSettings> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.ProviderSettings.SingleOrDefaultAsync(item => item.Id == 1, cancellationToken);
        if (settings is not null)
        {
            var normalizedUrl = AiProviderService.NormalizeApiBaseUrl(settings.ApiBaseUrl);
            if (settings.ApiBaseUrl != normalizedUrl)
            {
                settings.ApiBaseUrl = normalizedUrl;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return settings;
        }

        settings = new ProviderSettings();
        dbContext.ProviderSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private ProviderSettingsResponse ToResponse(ProviderSettings settings) =>
        new(
            settings.ProviderMode,
            settings.ApiBaseUrl,
            settings.ApiModel,
            !string.IsNullOrWhiteSpace(settings.ApiKeyEncrypted),
            aiProviderService.GetApiKeyType(settings),
            settings.LocalBaseUrl,
            settings.LocalModel);
}
