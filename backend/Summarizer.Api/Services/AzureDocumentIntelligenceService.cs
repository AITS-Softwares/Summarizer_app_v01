using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Text.Json;
using Summarizer.Api.Contracts;
using Summarizer.Api.Models;
using static Summarizer.Api.Services.ScreeningProcessorService;

namespace Summarizer.Api.Services;

public sealed record AzureScreeningOutcome(IReadOnlyCollection<SuggestedScreeningRow> Suggestions, string? ConfigurationMessage);

/// <summary>Uses Azure Document Intelligence Layout to OCR PDF pages without rendering them locally.</summary>
public sealed class AzureDocumentIntelligenceService(
    IHttpClientFactory httpClientFactory,
    AiProviderService aiProviderService,
    ILogger<AzureDocumentIntelligenceService> logger)
{
    private const string ApiVersion = "2024-11-30";
    private static readonly Regex BolNumberPattern = new(@"\b(?:bill\s+of\s+lading|b\s*/\s*l|bol)\s*(?:no\.?|number|num\.?|#)\s*[:#\-]?\s*([A-Z0-9][A-Z0-9./\-]{2,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BolTitlePattern = new(@"\b(?:bill\s+of\s+lading|b\s*/\s*l|bol\s+no\.?|bol\s+number)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FirstPagePattern = new(@"\b(?:page|pg\.?)\s*1\s*(?:of|/)\s*\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string NormalizeEndpoint(string? value) => value?.Trim().TrimEnd('/') ?? string.Empty;

    /// <summary>Uses Azure Layout OCR and app-side page markers to segment BOLs, then queries the fixed template headings.</summary>
    public async Task<AzureScreeningOutcome> ExtractScreeningAsync(
        string filePath, int sourceIndex, string sourceName,
        IReadOnlyCollection<TemplateRowDefinition> templateRows,
        ProviderSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.AzureDocumentIntelligenceEndpoint) || string.IsNullOrWhiteSpace(settings.AzureDocumentIntelligenceKeyEncrypted))
            return new AzureScreeningOutcome([], "Select Azure for Case Processing and configure its endpoint and key in Administration.");
        try
        {
            var client = CreateClient(settings);
            var layoutUrl = AnalyzeUrl(settings);
            using var layout = await SubmitFileAsync(client, layoutUrl, filePath, cancellationToken);
            if (layout is null) return new AzureScreeningOutcome([], "Azure Layout could not read this PDF. Check the endpoint, key, model, and service availability.");
            var pagesByNumber = ReadPages(layout.RootElement);
            if (pagesByNumber.Count == 0) return new AzureScreeningOutcome([], "Azure Layout did not return readable PDF pages.");
            var bolSegments = SegmentBolPages(pagesByNumber);

            var headings = templateRows.OrderBy(x => x.RowNumber).ToArray();
            var aliases = headings.Select((row, index) => (row, alias: $"Field{index + 1:00} {row.Heading}")).ToArray();
            var queryFields = string.Join(",", aliases.Select(x => x.alias));
            var all = new List<SuggestedScreeningRow>();
            foreach (var segment in bolSegments)
            {
                var pageNumbers = segment.PageNumbers;
                var bolIndex = segment.BolIndex;
                var pages = string.Join(",", pageNumbers);
                var modelUrl = $"{NormalizeEndpoint(settings.AzureDocumentIntelligenceEndpoint)}/documentintelligence/documentModels/{Uri.EscapeDataString(settings.AzureDocumentIntelligenceModel)}:analyze?api-version={ApiVersion}&pages={Uri.EscapeDataString(pages)}&features=queryFields&queryFields={Uri.EscapeDataString(queryFields)}";
                using var extracted = await SubmitFileAsync(client, modelUrl, filePath, cancellationToken);
                if (extracted is null) return new AzureScreeningOutcome([], $"Azure could not extract the requested fields for BOL {bolIndex}. Check the configured model and query-field support.");
                var fieldMap = ReadQueryFields(extracted.RootElement);
                foreach (var (row, alias) in aliases)
                {
                    fieldMap.TryGetValue(alias, out var value);
                    value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                    var type = value is null ? null : InferEntityType(row.Heading);
                    IReadOnlyDictionary<string, string>? fields = value is null ? null : new Dictionary<string, string> { ["Name*"] = value, ["Entity Type*"] = type ?? string.Empty };
                    all.Add(new SuggestedScreeningRow(sourceIndex, row.Heading, row.RowNumber, value, type, sourceName, pageNumbers[0], value is null ? 0m : 0.8m, value is null ? "not-found" : "needs-review", fields, bolIndex));
                }
            }
            if (all.Count == 0) return new AzureScreeningOutcome([], "Azure Layout found no BOL pages to process.");
            var hasBoundaryMarkers = pagesByNumber.Values.Any(text => BolNumberPattern.IsMatch(text) || FirstPagePattern.IsMatch(text));
            var warning = pagesByNumber.Count > 1 && bolSegments.Count == 1 && !hasBoundaryMarkers
                ? "Azure could not find BOL number or page-start markers, so it grouped this PDF as one BOL. Review the result if this PDF contains multiple BOLs."
                : null;
            return new AzureScreeningOutcome(all, warning);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Azure Document Intelligence Case Processing failed for {Source}", sourceName);
            return new AzureScreeningOutcome([], "Azure Case Processing could not be completed. Check the endpoint, key, model, and service availability.");
        }
    }

    private sealed record BolPageSegment(int BolIndex, List<int> PageNumbers);

    private static SortedDictionary<int, string> ReadPages(JsonElement root)
    {
        var pages = new SortedDictionary<int, string>();
        if (!root.TryGetProperty("analyzeResult", out var analyzed) || !analyzed.TryGetProperty("pages", out var pageArray) || pageArray.ValueKind != JsonValueKind.Array) return pages;
        foreach (var page in pageArray.EnumerateArray())
        {
            if (!page.TryGetProperty("pageNumber", out var numberElement) || !numberElement.TryGetInt32(out var number)) continue;
            var lines = new List<string>();
            if (page.TryGetProperty("lines", out var lineArray) && lineArray.ValueKind == JsonValueKind.Array)
                lines.AddRange(lineArray.EnumerateArray().Where(line => line.TryGetProperty("content", out _)).Select(line => line.GetProperty("content").GetString() ?? string.Empty));
            pages[number] = string.Join("\n", lines);
        }
        return pages;
    }

    private static List<BolPageSegment> SegmentBolPages(SortedDictionary<int, string> pages)
    {
        var segments = new List<BolPageSegment>();
        string? currentBolNumber = null;
        foreach (var (pageNumber, pageText) in pages)
        {
            var numberMatch = BolNumberPattern.Match(pageText);
            var pageBolNumber = numberMatch.Success ? numberMatch.Groups[1].Value.Trim().TrimEnd('.', ',') : null;
            var hasBolTitle = BolTitlePattern.IsMatch(pageText);
            var beginsNumberedDocument = hasBolTitle && FirstPagePattern.IsMatch(pageText);
            var startsNewBol = segments.Count == 0
                || (pageBolNumber is not null && currentBolNumber is not null && !pageBolNumber.Equals(currentBolNumber, StringComparison.OrdinalIgnoreCase))
                || (beginsNumberedDocument && segments.Count > 0);
            if (startsNewBol)
            {
                segments.Add(new BolPageSegment(segments.Count + 1, []));
                currentBolNumber = pageBolNumber;
            }
            else if (pageBolNumber is not null)
            {
                currentBolNumber ??= pageBolNumber;
            }
            segments[^1].PageNumbers.Add(pageNumber);
        }
        return segments;
    }

    private async Task<JsonDocument?> SubmitFileAsync(HttpClient client, string url, string filePath, CancellationToken token)
    {
        await using var stream = File.OpenRead(filePath);
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StreamContent(stream) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        using var submitted = await client.SendAsync(request, token);
        if (!submitted.IsSuccessStatusCode)
        {
            logger.LogWarning("Azure Document Intelligence request failed: {Status} {Reason}", (int)submitted.StatusCode, submitted.ReasonPhrase);
            return null;
        }
        if (!submitted.Headers.TryGetValues("operation-location", out var locations) || string.IsNullOrWhiteSpace(locations.FirstOrDefault())) return null;
        var resultUrl = locations.First();
        for (var attempt = 0; attempt < 120; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            using var response = await client.GetAsync(resultUrl, token);
            if (!response.IsSuccessStatusCode) return null;
            var payload = await response.Content.ReadAsStringAsync(token);
            var result = JsonDocument.Parse(payload);
            var status = result.RootElement.TryGetProperty("status", out var state) ? state.GetString() : null;
            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase)) return result;
            var failed = string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase);
            result.Dispose();
            if (failed) return null;
        }
        return null;
    }

    private static List<int> GetDocumentPages(JsonElement document)
    {
        var pages = new SortedSet<int>();
        if (document.TryGetProperty("boundingRegions", out var regions) && regions.ValueKind == JsonValueKind.Array)
            foreach (var region in regions.EnumerateArray())
                if (region.TryGetProperty("pageNumber", out var page) && page.TryGetInt32(out var number)) pages.Add(number);
        return pages.ToList();
    }

    private static Dictionary<string, string> ReadQueryFields(JsonElement root)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("analyzeResult", out var analyzed) || !analyzed.TryGetProperty("documents", out var docs) || docs.ValueKind != JsonValueKind.Array) return fields;
        foreach (var doc in docs.EnumerateArray())
            if (doc.TryGetProperty("fields", out var properties) && properties.ValueKind == JsonValueKind.Object)
                foreach (var property in properties.EnumerateObject())
                {
                    var value = FindFieldValue(property.Value);
                    if (!string.IsNullOrWhiteSpace(value)) fields[property.Name] = value;
                }
        return fields;
    }

    private static string? FindFieldValue(JsonElement field)
    {
        foreach (var name in new[] { "valueString", "valueDate", "valuePhoneNumber", "valueCountryRegion", "valueSelectionMark" })
            if (field.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
        if (field.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String) return content.GetString();
        return null;
    }

    public async Task<ProviderTestResponse> TestAsync(ProviderSettings settings, CancellationToken cancellationToken)
    {
        var endpoint = NormalizeEndpoint(settings.AzureDocumentIntelligenceEndpoint);
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(settings.AzureDocumentIntelligenceKeyEncrypted))
            return new ProviderTestResponse(false, "Configure an Azure endpoint and key first.", "Azure Document Intelligence", endpoint);
        try
        {
            var client = CreateClient(settings);
            if (settings.ScreeningProviderMode == "azure")
            {
                using var model = await client.GetAsync($"{endpoint}/documentintelligence/documentModels/{Uri.EscapeDataString(settings.AzureDocumentIntelligenceModel)}?api-version={ApiVersion}", cancellationToken);
                if (!model.IsSuccessStatusCode)
                    return new ProviderTestResponse(false, ToMessage("Azure could not find the configured field extraction model", model), "Azure Document Intelligence", endpoint);
                return new ProviderTestResponse(true, "Azure accepted the endpoint, key, and field extraction model. BOL page grouping will use document markers detected in each PDF.", "Azure Document Intelligence", endpoint);
            }
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

    private static string ToMessage(string prefix, HttpResponseMessage response) => $"{prefix} ({(int)response.StatusCode} {response.ReasonPhrase}).";
}
