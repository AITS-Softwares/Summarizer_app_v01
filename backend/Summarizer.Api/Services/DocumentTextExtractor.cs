using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Summarizer.Api.Services;

public sealed record DocumentExtractionResult(string? Text, string Status);

public sealed class DocumentTextExtractor(ILogger<DocumentTextExtractor> logger)
{
    private const int MaximumExtractedCharacters = 2_000_000;

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".csv", ".json", ".xml", ".html", ".htm",
        ".cs", ".ts", ".tsx", ".js", ".jsx", ".css", ".sql", ".log", ".yaml", ".yml"
    };

    public async Task<DocumentExtractionResult> ExtractAsync(
        string path,
        string originalName,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(originalName);

        try
        {
            if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return await Task.Run(
                    () => ExtractPdf(path, cancellationToken),
                    cancellationToken);
            }

            if (TextExtensions.Contains(extension))
            {
                return new DocumentExtractionResult(
                    await ExtractTextFileAsync(path, cancellationToken),
                    "ready");
            }

            return new DocumentExtractionResult(null, "unsupported");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Document text extraction failed for {Document}", originalName);
            return new DocumentExtractionResult(null, "failed");
        }
    }

    private static DocumentExtractionResult ExtractPdf(string path, CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(path);
        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageText = ContentOrderTextExtractor.GetText(page);
            if (string.IsNullOrWhiteSpace(pageText)) continue;

            builder.AppendLine($"--- Page {page.Number} ---");
            builder.AppendLine(pageText.Trim());
            builder.AppendLine();

            if (builder.Length >= MaximumExtractedCharacters)
            {
                break;
            }
        }

        if (builder.Length == 0)
        {
            return new DocumentExtractionResult(null, "ocr-required");
        }

        var text = builder.Length > MaximumExtractedCharacters
            ? builder.ToString(0, MaximumExtractedCharacters)
            : builder.ToString();
        return new DocumentExtractionResult(text, "ready");
    }

    private static async Task<string> ExtractTextFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        var buffer = new char[Math.Min(stream.Length, MaximumExtractedCharacters)];
        var count = await reader.ReadBlockAsync(buffer.AsMemory(), cancellationToken);
        return new string(buffer, 0, count);
    }
}
