using System.Text.Json;
using System.Diagnostics;
using UglyToad.PdfPig;
using Summarizer.Api.Models;

namespace Summarizer.Api.Services;

public sealed record ScannedScreeningSource(int Index, string Name, string StoragePath);
public sealed record VisionScreeningOutcome(IReadOnlyCollection<SuggestedScreeningRow> Suggestions, string? ConfigurationMessage);

public sealed class VisionScreeningExtractor(
    AiProviderService aiProviderService,
    IConfiguration configuration,
    ILogger<VisionScreeningExtractor> logger)
{
    private const int MaximumPages = 30;
    private const int MaximumImageBytes = 25 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<VisionScreeningOutcome> ExtractAsync(
        IReadOnlyCollection<TemplateRowDefinition> templateRows,
        ScannedScreeningSource source,
        ProviderSettings settings,
        CancellationToken cancellationToken)
    {
        var pageResult = await ExtractPageImages([source], cancellationToken);
        if (pageResult.ConfigurationMessage is not null)
            return new VisionScreeningOutcome([], pageResult.ConfigurationMessage);
        var pages = pageResult.Pages;
        if (pages.Count == 0)
        {
            return new VisionScreeningOutcome([], "The PDF renderer could not create page images. Install Poppler (pdftoppm) or configure Ocr:PdfToImageCommand, then process the document again.");
        }
        if (pages.Count == 1)
            return await ExtractMultipleBolsFromPageAsync(templateRows, source, settings, pages[0], cancellationToken);

        var split = await FindBolsAsync(source, settings, pages, cancellationToken);
        if (split.ConfigurationMessage is not null)
            return new VisionScreeningOutcome([], split.ConfigurationMessage);
        var bols = split.Ranges;
        var suggestions = new List<SuggestedScreeningRow>();
        for (var i = 0; i < bols.Count; i++)
        {
            var bolPages = pages.Skip(bols[i].StartPage - 1).Take(bols[i].EndPage - bols[i].StartPage + 1).ToArray();
            var outcome = await ExtractBolAsync(templateRows, source, settings, bolPages, i + 1, cancellationToken);
            if (outcome.ConfigurationMessage is not null) return new VisionScreeningOutcome([], outcome.ConfigurationMessage);
            suggestions.AddRange(outcome.Suggestions);
        }
        return new VisionScreeningOutcome(suggestions, null);
    }

    private async Task<VisionScreeningOutcome> ExtractMultipleBolsFromPageAsync(
        IReadOnlyCollection<TemplateRowDefinition> templateRows,
        ScannedScreeningSource source,
        ProviderSettings settings,
        PageImage page,
        CancellationToken cancellationToken)
    {
        var values = new List<VisionRowValue>();
        foreach (var rowBatch in templateRows.Chunk(5))
        {
            var result = await aiProviderService.AnalyzeImagesAsync(
                settings,
                settings.ScreeningProviderMode,
                BuildSinglePageBolPrompt(rowBatch, source.Name, page.PageNumber),
                [page.Image],
                cancellationToken);
            if (result.RequiresConfiguration) return new VisionScreeningOutcome([], result.Content);
            values.AddRange(ParseValues(result.Content));
        }

        var validValues = values
            .Where(value => value.BolIndex > 0 && templateRows.Any(row => row.RowNumber == value.TemplateRowNumber))
            .GroupBy(value => (value.BolIndex, value.TemplateRowNumber))
            .Select(group => group.First())
            .ToArray();
        if (validValues.Length == 0)
            return new VisionScreeningOutcome([], "The scanned page was read but no BOL fields could be extracted. Check the selected vision model and retry.");

        var suggestions = validValues.Select(value =>
        {
            var row = templateRows.First(item => item.RowNumber == value.TemplateRowNumber);
            var name = value.EntityName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return new SuggestedScreeningRow(source.Index, row.Heading, row.RowNumber, null, null, source.Name, page.PageNumber, 0m, "not-found", BolIndex: value.BolIndex);

            var type = NormalizeEntityType(value.EntityTypeCode) ?? ScreeningProcessorService.InferEntityType(row.Heading);
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Name*"] = name, ["Entity Type*"] = type };
            return new SuggestedScreeningRow(source.Index, row.Heading, row.RowNumber, name, type, source.Name, page.PageNumber, Math.Clamp(value.Confidence ?? 0.7m, 0m, 1m), "needs-review", fields, value.BolIndex);
        }).ToArray();

        var bolIndexes = validValues.Select(value => value.BolIndex).Distinct().Order().ToArray();
        return new VisionScreeningOutcome(bolIndexes.SelectMany(bolIndex => templateRows.Select(row =>
        {
            var found = suggestions.FirstOrDefault(item => item.BolIndex == bolIndex && item.TemplateRowNumber == row.RowNumber);
            return found ?? new SuggestedScreeningRow(source.Index, row.Heading, row.RowNumber, null, null, source.Name, page.PageNumber, 0m, "not-found", BolIndex: bolIndex);
        })).ToArray(), null);
    }

    public async Task<VisionScreeningOutcome> ExtractTextAsync(
        IReadOnlyCollection<TemplateRowDefinition> templateRows,
        ScreeningSource source,
        ProviderSettings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(source.Text))
            return new VisionScreeningOutcome([], "The PDF did not contain readable text for BOL extraction.");

        var result = await aiProviderService.AnalyzeAsync(
            settings,
            settings.ScreeningProviderMode,
            BuildTextPrompt(templateRows, source.Name),
            source.Text,
            cancellationToken);
        if (result.RequiresConfiguration) return new VisionScreeningOutcome([], result.Content);

        var values = ParseValues(result.Content)
            .Where(value => value.BolIndex > 0 && templateRows.Any(row => row.RowNumber == value.TemplateRowNumber))
            .GroupBy(value => (value.BolIndex, value.TemplateRowNumber))
            .Select(group => group.First())
            .ToArray();
        if (values.Length == 0)
        {
            logger.LogWarning("Text extraction returned no valid BOL rows for {Document}. Response prefix: {Response}", source.Name, result.Content[..Math.Min(result.Content.Length, 300)]);
            return new VisionScreeningOutcome([], "The document was read but no BOL records could be extracted. Check the selected AI model and retry.");
        }

        var bolIndexes = values.Select(item => item.BolIndex).Distinct().Order().ToArray();
        var suggestions = bolIndexes.SelectMany(bolIndex => templateRows.Select(row =>
        {
            var value = values.FirstOrDefault(item => item.BolIndex == bolIndex && item.TemplateRowNumber == row.RowNumber);
            var entityName = value?.EntityName?.Trim();
            if (string.IsNullOrWhiteSpace(entityName))
                return new SuggestedScreeningRow(source.Index, row.Heading, row.RowNumber, null, null, source.Name, value?.SourcePageNumber, 0m, "not-found", BolIndex: bolIndex);

            var entityType = NormalizeEntityType(value?.EntityTypeCode) ?? ScreeningProcessorService.InferEntityType(row.Heading);
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name*"] = entityName,
                ["Entity Type*"] = entityType
            };
            return new SuggestedScreeningRow(source.Index, row.Heading, row.RowNumber, entityName, entityType, source.Name, value?.SourcePageNumber, Math.Clamp(value?.Confidence ?? 0.7m, 0m, 1m), "needs-review", fields, bolIndex);
        })).ToArray();
        return new VisionScreeningOutcome(suggestions, null);
    }

    private async Task<(IReadOnlyList<BolRange> Ranges, string? ConfigurationMessage)> FindBolsAsync(
        ScannedScreeningSource source,
        ProviderSettings settings,
        IReadOnlyList<PageImage> pages,
        CancellationToken cancellationToken)
    {
        var wholeDocument = new[] { new BolRange(1, pages.Count) };
        if (pages.Count == 1) return (wholeDocument, null);
        try
        {
            var result = await aiProviderService.AnalyzeImagesAsync(
                settings,
                settings.ScreeningProviderMode,
                BuildBolSplitPrompt(source.Name, pages.Count),
                pages.Select(page => page.Image).ToArray(),
                cancellationToken);
            if (result.RequiresConfiguration) return ([], result.Content);
            var ranges = ParseBolRanges(result.Content, pages.Count);
            if (ranges.Count == 0)
                return ([], "The scanned PDF was read, but the system could not separate its BOLs reliably. Try again or use a searchable PDF.");
            return (ranges, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "BOL splitting failed for {Document}", source.Name);
            return ([], "The scanned PDF could not be segmented into BOLs. Check the AI provider settings and retry.");
        }
    }

    private async Task<VisionScreeningOutcome> ExtractBolAsync(
        IReadOnlyCollection<TemplateRowDefinition> templateRows,
        ScannedScreeningSource source,
        ProviderSettings settings,
        IReadOnlyList<PageImage> bolPages,
        int bolIndex,
        CancellationToken cancellationToken)
    {
        var images = bolPages.Select(page => page.Image).ToArray();
        var pageNumbers = bolPages.Select(page => page.PageNumber).ToArray();
        var values = new List<VisionRowValue>();
        foreach (var rowBatch in templateRows.Chunk(5))
        {
            var result = await aiProviderService.AnalyzeImagesAsync(
                settings,
                settings.ScreeningProviderMode,
                BuildPrompt(rowBatch, [source.Name], pageNumbers),
                images,
                cancellationToken);
            if (result.RequiresConfiguration)
            {
                logger.LogWarning("Vision extraction was not available: {Reason}", result.Content);
                return new VisionScreeningOutcome([], result.Content);
            }

            var parsed = ParseValues(result.Content);
            if (parsed.Count == 0)
            {
                logger.LogWarning("Vision extraction returned no valid JSON rows for BOL {Bol} of {Document}, headings: {Headings}. Response prefix: {Response}", bolIndex, source.Name, string.Join(", ", rowBatch.Select(row => row.Heading)), result.Content[..Math.Min(result.Content.Length, 300)]);
                continue;
            }
            values.AddRange(parsed);
        }

        if (values.Count == 0)
        {
            return new VisionScreeningOutcome([], "The AI completed but returned no valid extracted fields. Verify that the selected model supports document vision and retry.");
        }

        var suggestions = templateRows.Select(templateRow =>
        {
            var value = values.FirstOrDefault(item => item.TemplateRowNumber == templateRow.RowNumber);
            var entityName = value?.EntityName?.Trim();
            if (value is null || string.IsNullOrWhiteSpace(entityName))
            {
                return new SuggestedScreeningRow(source.Index, templateRow.Heading, templateRow.RowNumber, null, null, source.Name, null, 0m, "not-found", null, bolIndex);
            }

            var entityType = NormalizeEntityType(value.EntityTypeCode) ?? ScreeningProcessorService.InferEntityType(templateRow.Heading);
            int? pageNumber = value.SourcePageNumber is int reported && pageNumbers.Contains(reported) ? reported : null;
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name*"] = entityName,
                ["Entity Type*"] = entityType
            };
            return new SuggestedScreeningRow(
                source.Index,
                templateRow.Heading,
                templateRow.RowNumber,
                entityName,
                entityType,
                source.Name,
                pageNumber,
                Math.Clamp(value.Confidence ?? 0.7m, 0m, 1m),
                "needs-review",
                fields,
                bolIndex);
        }).ToArray();
        return new VisionScreeningOutcome(suggestions, null);
    }

    private async Task<(IReadOnlyList<PageImage> Pages, string? ConfigurationMessage)> ExtractPageImages(IReadOnlyCollection<ScannedScreeningSource> sources, CancellationToken cancellationToken)
    {
        var images = new List<PageImage>();
        var totalBytes = 0;
        foreach (var source in sources)
        {
            using (var document = PdfDocument.Open(source.StoragePath))
            {
                if (document.NumberOfPages > MaximumPages)
                    return ([], $"{source.Name} has {document.NumberOfPages} pages. Scanned BOL extraction currently supports up to {MaximumPages} pages per PDF.");
            }

            foreach (var page in await RenderPdfPagesAsync(source, cancellationToken))
            {
                if (images.Count >= MaximumPages || totalBytes + page.Bytes.Length > MaximumImageBytes)
                    return ([], $"{source.Name} exceeds the scanned-image processing limit. Reduce its resolution or split the PDF, then retry.");
                totalBytes += page.Bytes.Length;
                images.Add(new PageImage(page.PageNumber, new AiImage(source.Name, page.PageNumber, page.MediaType, Convert.ToBase64String(page.Bytes))));
            }
        }
        return (images, null);
    }

    private async Task<IReadOnlyCollection<RenderedPdfPage>> RenderPdfPagesAsync(ScannedScreeningSource source, CancellationToken cancellationToken)
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"summary-studio-ocr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        try
        {
            var prefix = Path.Combine(workingDirectory, "page");
            var command = ResolvePdfToImageCommand();
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-jpeg");
            startInfo.ArgumentList.Add("-r");
            startInfo.ArgumentList.Add("150");
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add("1");
            startInfo.ArgumentList.Add("-l");
            startInfo.ArgumentList.Add(MaximumPages.ToString());
            startInfo.ArgumentList.Add(source.StoragePath);
            startInfo.ArgumentList.Add(prefix);
            using var process = Process.Start(startInfo);
            if (process is null) return [];
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                logger.LogWarning("PDF-to-image conversion failed for {Document}: {Error}", source.Name, error);
                return ExtractEmbeddedPdfImages(source, cancellationToken);
            }

            return Directory.GetFiles(workingDirectory, "page-*.jpg")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select((path, index) => new RenderedPdfPage(index + 1, "image/jpeg", File.ReadAllBytes(path)))
                .Where(page => page.Bytes.Length > 0)
                .ToArray();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "PDF-to-image conversion was unavailable for {Document}", source.Name);
            return ExtractEmbeddedPdfImages(source, cancellationToken);
        }
        finally
        {
            if (Directory.Exists(workingDirectory)) Directory.Delete(workingDirectory, recursive: true);
        }
    }

    private static IReadOnlyCollection<RenderedPdfPage> ExtractEmbeddedPdfImages(ScannedScreeningSource source, CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(source.StoragePath);
        return document.GetPages()
            .Take(MaximumPages)
            .Select(page =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var image = page.GetImages().OrderByDescending(item => (long)item.WidthInSamples * item.HeightInSamples).FirstOrDefault();
                return image is not null && image.TryGetPng(out var pngBytes) && pngBytes.Length > 0
                    ? new RenderedPdfPage(page.Number, "image/png", pngBytes)
                    : null;
            })
            .Where(page => page is not null)
            .Select(page => page!)
            .ToArray();
    }

    private string ResolvePdfToImageCommand()
    {
        var configured = configuration["Ocr:PdfToImageCommand"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "tools", "poppler", "pdftoppm.exe"),
            Path.Combine(AppContext.BaseDirectory, "pdftoppm.exe"),
            Path.Combine(userProfile, ".cache", "codex-runtimes", "codex-primary-runtime", "dependencies", "native", "poppler", "Library", "bin", "pdftoppm.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "poppler", "Library", "bin", "pdftoppm.exe")
        };
        return candidates.FirstOrDefault(File.Exists) ?? "pdftoppm";
    }

    private static string BuildBolSplitPrompt(string sourceName, int pageCount) =>
        string.Join(Environment.NewLine,
        [
            $"The images are the pages of one PDF named {sourceName}, in order. There are {pageCount} pages, numbered 1 to {pageCount} by image order.",
            "The PDF may contain several separate Bills of Lading (BOLs) one after another. One BOL can span several pages.",
            "Identify every separate BOL. Return JSON only, in this exact shape:",
            "{\"bols\":[{\"startPage\":1,\"endPage\":2},{\"startPage\":3,\"endPage\":3}]}",
            "Rules:",
            "- List BOLs in page order, with no overlapping pages.",
            "- A continuation page belongs to the BOL before it.",
            "- Leave out pages that are not part of any BOL, such as cover sheets.",
            "- If there is only one BOL, return a single entry."
        ]);

    private static string BuildSinglePageBolPrompt(IReadOnlyCollection<TemplateRowDefinition> rows, string sourceName, int pageNumber) =>
        string.Join(Environment.NewLine,
        [
            $"The image is page {pageNumber} of {sourceName}. It may contain one or several separate Bills of Lading (BOLs).",
            "Extract each BOL separately in visual reading order. For every BOL, return the visible value for each requested template heading. Do not combine values across BOLs or invent missing values.",
            "Return JSON only in this shape:",
            "{\"rows\":[{\"bolIndex\":1,\"templateRowNumber\":2,\"entityName\":\"exact visible name\",\"entityTypeCode\":\"O\",\"sourcePageNumber\":1,\"confidence\":0.95}]}",
            "Use bolIndex 1, 2, 3... in the order the BOLs appear. Repeat each templateRowNumber for every BOL where the heading is visible.",
            "Requested template headings:",
            string.Join(Environment.NewLine, rows.GroupBy(row => row.Heading, StringComparer.OrdinalIgnoreCase).SelectMany(group => group.Select((row, index) => $"- templateRowNumber {row.RowNumber}: {row.Heading} (occurrence {index + 1})")))
        ]);

    private static string BuildPrompt(IReadOnlyCollection<TemplateRowDefinition> rows, IEnumerable<string> sourceNames, IReadOnlyCollection<int> pageNumbers) =>
        string.Join(Environment.NewLine,
        [
            "Extract exactly one visible entity name for each requested heading from the scanned source documents. Each image belongs to a source file and page. Do not include addresses, identifiers, or other details in the name.",
            string.Empty,
            $"Source files: {string.Join(", ", sourceNames)}",
            $"The images are PDF pages {string.Join(", ", pageNumbers)}, in that order. Together they are one single Bill of Lading. Report sourcePageNumber as the PDF page number from this list.",
            string.Empty,
            "Return JSON only, with this exact shape:",
            "{\"rows\":[{\"templateRowNumber\":2,\"entityName\":\"exact visible name\",\"entityTypeCode\":\"O\",\"sourceFileName\":\"source.pdf\",\"sourcePageNumber\":1,\"confidence\":0.95}]}",
            string.Empty,
            "Type codes: I = individual, O = organisation, V = vessel, U = location or country.",
            "Rules:",
            "- Return at most one object for each requested template row number.",
            "- Return one object for every requested row where the entity name is visible.",
            "- Return only entityName and entityTypeCode. Do not return fields or any other document data.",
            "- Never use information outside the scanned images.",
            string.Empty,
            "Requested template rows:",
            string.Join(Environment.NewLine, rows.GroupBy(row => row.Heading, StringComparer.OrdinalIgnoreCase).SelectMany(group => group.Select((row, index) => $"- {row.RowNumber}: {row.Heading} (occurrence {index + 1})")))
        ]);

    private static string BuildTextPrompt(IReadOnlyCollection<TemplateRowDefinition> rows, string sourceName) =>
        string.Join(Environment.NewLine,
        [
            $"Extract entities from the complete PDF named {sourceName}. The supplied text includes page markers.",
            "The PDF may contain multiple separate Bills of Lading (BOLs), and one BOL may span multiple pages. Identify every BOL, preserve their order, and keep their values separate.",
            "For each BOL, extract the visible value for every requested template heading. Do not merge BOLs or invent missing values.",
            "Return JSON only in this exact shape:",
            "{\"rows\":[{\"bolIndex\":1,\"templateRowNumber\":2,\"entityName\":\"exact visible name\",\"entityTypeCode\":\"O\",\"sourcePageNumber\":1,\"confidence\":0.95}]}",
            "Assign bolIndex values 1, 2, 3... in the order each BOL starts. Repeat each templateRowNumber for each BOL where that heading is present. sourcePageNumber is the source PDF page containing the value.",
            "Return only entity names and type codes. Do not put addresses, identifiers, or other details in entityName.",
            "Requested template headings:",
            string.Join(Environment.NewLine, rows.Select(row => $"- templateRowNumber {row.RowNumber}: {row.Heading}"))
        ]);

    private static string? NormalizeEntityType(string? value)
    {
        var code = value?.Trim().ToUpperInvariant();
        return code is "I" or "O" or "V" or "U" ? code : null;
    }

    private static string? ExtractJsonObject(string response)
    {
        var json = response.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            json = json[(json.IndexOf('\n') + 1)..];
            var closingFence = json.LastIndexOf("```", StringComparison.Ordinal);
            if (closingFence >= 0) json = json[..closingFence];
        }
        var first = json.IndexOf('{');
        var last = json.LastIndexOf('}');
        return first < 0 || last <= first ? null : json[first..(last + 1)];
    }

    private static IReadOnlyList<BolRange> ParseBolRanges(string response, int pageCount)
    {
        var json = ExtractJsonObject(response);
        if (json is null) return [];
        try
        {
            var payload = JsonSerializer.Deserialize<BolSplitResponse>(json, JsonOptions);
            var ranges = payload?.Bols?
                .Where(bol => bol.StartPage >= 1 && bol.StartPage <= pageCount && bol.EndPage >= bol.StartPage)
                .Select(bol => new BolRange(bol.StartPage, Math.Min(bol.EndPage, pageCount)))
                .OrderBy(range => range.StartPage)
                .ToArray() ?? [];
            return ranges.Zip(ranges.Skip(1), (current, next) => current.EndPage >= next.StartPage).Any(overlaps => overlaps) ? [] : ranges;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyCollection<VisionRowValue> ParseValues(string response)
    {
        var json = ExtractJsonObject(response);
        if (json is null) return [];

        try
        {
            var payload = JsonSerializer.Deserialize<VisionResponse>(json, JsonOptions);
            return payload?.Rows?.Where(row => row.TemplateRowNumber > 0).ToArray() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed class VisionResponse
    {
        public IReadOnlyCollection<VisionRowValue>? Rows { get; init; }
    }

    private sealed class VisionRowValue
    {
        public int BolIndex { get; init; } = 1;
        public int TemplateRowNumber { get; init; }
        public string? EntityName { get; init; }
        public Dictionary<string, string>? Fields { get; init; }
        public string? EntityTypeCode { get; init; }
        public string? SourceFileName { get; init; }
        public int? SourcePageNumber { get; init; }
        public decimal? Confidence { get; init; }
    }

    private sealed class BolSplitResponse
    {
        public IReadOnlyCollection<BolSegment>? Bols { get; init; }
    }

    private sealed class BolSegment
    {
        public int StartPage { get; init; }
        public int EndPage { get; init; }
    }

    private sealed record PageImage(int PageNumber, AiImage Image);
    private sealed record BolRange(int StartPage, int EndPage);
    private sealed record RenderedPdfPage(int PageNumber, string MediaType, byte[] Bytes);
}
