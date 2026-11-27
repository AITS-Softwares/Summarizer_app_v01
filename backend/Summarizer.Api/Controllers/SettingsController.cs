using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Models;
using Summarizer.Api.Services;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Policy = "SystemAdministration")]
public sealed class SettingsController(AppDbContext dbContext, AiProviderService aiProviderService, AzureDocumentIntelligenceService azureDocumentIntelligenceService, AuditService auditService) : ControllerBase
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
        if (request.ScreeningProviderMode is not ("api" or "local" or "azure")) return BadRequest("ScreeningProviderMode must be 'api', 'local', or 'azure'.");
        if (request.OcrProviderMode is not ("ai" or "azure")) return BadRequest("OcrProviderMode must be 'ai' or 'azure'.");

        var settings = await GetSettingsAsync(cancellationToken);
        settings.ProviderMode = request.ProviderMode;
        settings.ScreeningProviderMode = request.ScreeningProviderMode;
        settings.ApiBaseUrl = AiProviderService.NormalizeApiBaseUrl(request.ApiBaseUrl);
        settings.ApiModel = request.ApiModel.Trim();
        settings.LocalBaseUrl = request.LocalBaseUrl.Trim().TrimEnd('/');
        settings.LocalModel = request.LocalModel.Trim();
        settings.OcrProviderMode = request.OcrProviderMode;
        settings.AzureDocumentIntelligenceEndpoint = AzureDocumentIntelligenceService.NormalizeEndpoint(request.AzureDocumentIntelligenceEndpoint);
        settings.AzureDocumentIntelligenceModel = string.IsNullOrWhiteSpace(request.AzureDocumentIntelligenceModel) ? "prebuilt-layout" : request.AzureDocumentIntelligenceModel.Trim();
        settings.AzureUseAiFallback = request.AzureUseAiFallback;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        if (request.ClearApiKey)
        {
            settings.ApiKeyEncrypted = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            settings.ApiKeyEncrypted = aiProviderService.ProtectApiKey(request.ApiKey.Trim());
        }
        if (request.ClearAzureDocumentIntelligenceKey) settings.AzureDocumentIntelligenceKeyEncrypted = null;
        else if (!string.IsNullOrWhiteSpace(request.AzureDocumentIntelligenceKey)) settings.AzureDocumentIntelligenceKeyEncrypted = aiProviderService.ProtectApiKey(request.AzureDocumentIntelligenceKey.Trim());

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync("configuration.provider.updated", targetType: "provider-settings", targetId: settings.Id.ToString(), detail: $"AI mode: {settings.ProviderMode}; screening: {settings.ScreeningProviderMode}", cancellationToken: cancellationToken);
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

        await auditService.RecordAsync("configuration.provider.tested", result.RequiresConfiguration ? "failed" : "success", "provider-settings", settings.Id.ToString(), providerName, cancellationToken);

        return Ok(new ProviderTestResponse(
            !result.RequiresConfiguration,
            result.RequiresConfiguration ? result.Content : "Connection successful. The provider accepted the configured key and model.",
            providerName,
            endpoint));
    }

    [HttpPost("test-ocr")]
    public async Task<ActionResult<ProviderTestResponse>> TestOcr(CancellationToken cancellationToken)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        if (settings.ScreeningProviderMode != "azure")
            return BadRequest("Select Azure for OCR or Case Processing before testing its connection.");
        var result = await azureDocumentIntelligenceService.TestAsync(settings, cancellationToken);
        await auditService.RecordAsync("configuration.ocr.tested", result.Success ? "success" : "failed", "provider-settings", settings.Id.ToString(), "Azure Document Intelligence", cancellationToken);
        return Ok(result);
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
            settings.ScreeningProviderMode,
            settings.ApiBaseUrl,
            settings.ApiModel,
            !string.IsNullOrWhiteSpace(settings.ApiKeyEncrypted),
            aiProviderService.GetApiKeyType(settings),
            settings.LocalBaseUrl,
            settings.LocalModel,
            settings.OcrProviderMode,
            settings.AzureDocumentIntelligenceEndpoint,
            !string.IsNullOrWhiteSpace(settings.AzureDocumentIntelligenceKeyEncrypted),
            settings.AzureDocumentIntelligenceModel,
            settings.AzureUseAiFallback);
}
