using System.Text.Json;
using System.Diagnostics;
using UglyToad.PdfPig;
using Summarizer.Api.Models;

namespace Summarizer.Api.Services;

public sealed record ScannedScreeningSource(string Name, string StoragePath);
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
        IReadOnlyCollection<ScannedScreeningSource> sources,
        ProviderSettings settings,
        CancellationToken cancellationToken)
    {
        var images = await ExtractPageImages(sources, cancellationToken);
        if (images.Count == 0)
        {
            return new VisionScreeningOutcome([], "The PDF renderer could not create page images. Install Poppler (pdftoppm) or configure Ocr:PdfToImageCommand, then process the document again.");
        }

        var values = new List<VisionRowValue>();
        foreach (var rowBatch in templateRows.Chunk(5))
        {
            var result = await aiProviderService.AnalyzeImagesAsync(
                settings,
                settings.ProviderMode,
                BuildPrompt(rowBatch, sources.Select(source => source.Name)),
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
                logger.LogWarning("Vision extraction returned no valid JSON rows for headings: {Headings}. Response prefix: {Response}", string.Join(", ", rowBatch.Select(row => row.Heading)), result.Content[..Math.Min(result.Content.Length, 300)]);
                return new VisionScreeningOutcome([], "The AI completed but returned no valid extracted fields. Verify that the selected model supports document vision and retry.");
            }
            values.AddRange(parsed);
        }

        var knownSourceNames = sources.Select(source => source.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var suggestions = templateRows.Select(templateRow =>
        {
            var value = values.FirstOrDefault(item => item.TemplateRowNumber == templateRow.RowNumber);
            if (value is null)
            {
                return new SuggestedScreeningRow(templateRow.Heading, templateRow.RowNumber, null, null, null, null, 0m, "not-found");
            }

            var entityName = value.EntityName?.Trim();
            if (string.IsNullOrWhiteSpace(entityName)) return new SuggestedScreeningRow(templateRow.Heading, templateRow.RowNumber, null, null, null, null, 0m, "not-found");
            var sourceName = !string.IsNullOrWhiteSpace(value.SourceFileName) && knownSourceNames.Contains(value.SourceFileName)
                ? value.SourceFileName
                : sources.First().Name;
            var confidence = Math.Clamp(value.Confidence ?? 0.7m, 0m, 1m);
            var entityType = NormalizeEntityType(value.EntityTypeCode) ?? ScreeningProcessorService.InferEntityType(templateRow.Heading);
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name*"] = entityName,
                ["Entity Type*"] = entityType
            };
            var status = "needs-review";
            return new SuggestedScreeningRow(
                templateRow.Heading,
                templateRow.RowNumber,
                entityName,
                entityType,
                sourceName,
                value.SourcePageNumber is > 0 ? value.SourcePageNumber : null,
                confidence,
                status,
                fields);
        }).ToArray();
        return new VisionScreeningOutcome(suggestions, null);
    }

    private async Task<IReadOnlyCollection<AiImage>> ExtractPageImages(IReadOnlyCollection<ScannedScreeningSource> sources, CancellationToken cancellationToken)
    {
        var images = new List<AiImage>();
        var totalBytes = 0;
        foreach (var source in sources)
        {
            foreach (var page in await RenderPdfPagesAsync(source, cancellationToken))
            {
                if (images.Count >= MaximumPages || totalBytes >= MaximumImageBytes) return images;
                if (totalBytes + page.Bytes.Length > MaximumImageBytes) continue;
                totalBytes += page.Bytes.Length;
                images.Add(new AiImage(source.Name, page.PageNumber, page.MediaType, Convert.ToBase64String(page.Bytes)));
            }
        }
        return images;
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

    private static string BuildPrompt(IReadOnlyCollection<TemplateRowDefinition> rows, IEnumerable<string> sourceNames) =>
        string.Join(Environment.NewLine,
        [
            "Extract exactly one visible entity name for each requested heading from the scanned source documents. Each image belongs to a source file and page. Do not include addresses, identifiers, or other details in the name.",
            string.Empty,
            $"Source files: {string.Join(", ", sourceNames)}",
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

    private static string? NormalizeEntityType(string? value)
    {
        var code = value?.Trim().ToUpperInvariant();
        return code is "I" or "O" or "V" or "U" ? code : null;
    }

    private static IReadOnlyCollection<VisionRowValue> ParseValues(string response)
    {
        var json = response.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            json = json[(json.IndexOf('\n') + 1)..];
            var closingFence = json.LastIndexOf("```", StringComparison.Ordinal);
            if (closingFence >= 0) json = json[..closingFence];
        }
        var firstBrace = json.IndexOf('{');
        var lastBrace = json.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace <= firstBrace) return [];

        try
        {
            var payload = JsonSerializer.Deserialize<VisionResponse>(json[firstBrace..(lastBrace + 1)], JsonOptions);
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
        public int TemplateRowNumber { get; init; }
        public string? EntityName { get; init; }
        public Dictionary<string, string>? Fields { get; init; }
        public string? EntityTypeCode { get; init; }
        public string? SourceFileName { get; init; }
        public int? SourcePageNumber { get; init; }
        public decimal? Confidence { get; init; }
    }

    private sealed record RenderedPdfPage(int PageNumber, string MediaType, byte[] Bytes);
}
