using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Summarizer.Api.Models;

namespace Summarizer.Api.Services;

public sealed record TemplateRowDefinition(int RowNumber, string Heading);
public sealed record TemplateDefinition(string WorksheetName, int HeaderRowNumber, IReadOnlyCollection<TemplateRowDefinition> Rows, IReadOnlyDictionary<string, int> Columns);
public sealed record ScreeningSource(string Name, string? Text, string Status);
public sealed record SuggestedScreeningRow(string Heading, int TemplateRowNumber, string? EntityName, string? EntityTypeCode, string? SourceFileName, int? SourcePageNumber, decimal Confidence, string Status, IReadOnlyDictionary<string, string>? Fields = null);

public sealed class ScreeningProcessorService
{
    public TemplateDefinition ReadTemplate(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidDataException("The workbook has no worksheet.");
        var headerRow = FindHeaderRow(sheet);
        var headingColumn = FindColumn(sheet, headerRow, "heading");
        if (headingColumn == 0 || FindColumn(sheet, headerRow, "name") == 0 || FindColumn(sheet, headerRow, "entitytype") == 0)
        {
            throw new InvalidDataException("The template needs Heading, Name*, and Entity Type* columns.");
        }

        var rows = sheet.RowsUsed()
            .Where(row => row.RowNumber() > headerRow)
            .Select(row => new TemplateRowDefinition(row.RowNumber(), row.Cell(headingColumn).GetString().Trim()))
            .Where(row => !string.IsNullOrWhiteSpace(row.Heading))
            .ToArray();
        if (rows.Length == 0) throw new InvalidDataException("The template has no headings below its header row.");

        var columns = sheet.Row(headerRow).CellsUsed().ToDictionary(cell => cell.GetString().Trim(), cell => cell.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);
        return new TemplateDefinition(sheet.Name, headerRow, rows, columns);
    }

    public IReadOnlyCollection<SuggestedScreeningRow> SuggestRows(
        IReadOnlyCollection<TemplateRowDefinition> templateRows,
        IReadOnlyCollection<ScreeningSource> sources)
    {
        var hasOcrOnlySource = sources.Any(source => source.Status == "ocr-required") && sources.All(source => string.IsNullOrWhiteSpace(source.Text));
        return templateRows.Select(row => SuggestRow(row, sources, hasOcrOnlySource)).ToArray();
    }

    public string ExportCompletedTemplate(ScreeningTemplate template, IReadOnlyCollection<ScreeningRow> rows, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        using var workbook = new XLWorkbook(template.StoragePath);
        var sheet = workbook.Worksheet(template.WorksheetName);
        var headingColumn = FindColumn(sheet, template.HeaderRowNumber, "heading");

        foreach (var row in rows)
        {
            var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(row.ExtractedFieldsJson) ?? [];
            foreach (var cell in sheet.Row(template.HeaderRowNumber).CellsUsed())
            {
                var header = cell.GetString().Trim();
                if (header.Equals("Heading", StringComparison.OrdinalIgnoreCase)) continue;
                var outputCell = sheet.Cell(row.TemplateRowNumber, cell.Address.ColumnNumber);
                outputCell.Clear(XLClearOptions.Contents);
                if (header.Equals("Name*", StringComparison.OrdinalIgnoreCase) && fields.TryGetValue("Name*", out var name)) outputCell.Value = name;
                if (header.Equals("Entity Type*", StringComparison.OrdinalIgnoreCase) && fields.TryGetValue("Entity Type*", out var entityType)) outputCell.Value = entityType;
            }
        }
        if (headingColumn > 0) sheet.Column(headingColumn).Delete();

        var outputPath = Path.Combine(outputDirectory, $"screening-result-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.xlsx");
        workbook.SaveAs(outputPath);
        return outputPath;
    }

    public static IReadOnlyCollection<TemplateRowDefinition> DeserializeRows(string rowsJson) =>
        JsonSerializer.Deserialize<List<TemplateRowDefinition>>(rowsJson) ?? [];

    public static string SerializeRows(IReadOnlyCollection<TemplateRowDefinition> rows) => JsonSerializer.Serialize(rows);

    private static SuggestedScreeningRow SuggestRow(TemplateRowDefinition row, IReadOnlyCollection<ScreeningSource> sources, bool hasOcrOnlySource)
    {
        foreach (var source in sources.Where(item => !string.IsNullOrWhiteSpace(item.Text)))
        {
            var value = ExtractValueAfterHeading(source.Text!, row.Heading);
            if (!string.IsNullOrWhiteSpace(value))
            {
                var entityType = InferEntityType(row.Heading);
                return new SuggestedScreeningRow(row.Heading, row.RowNumber, value, entityType, source.Name, FindPageNumber(source.Text!, row.Heading), 0.55m, "needs-review", new Dictionary<string, string> { ["Name*"] = value, ["Entity Type*"] = entityType });
            }
        }

        return new SuggestedScreeningRow(row.Heading, row.RowNumber, null, null, null, null, 0m, hasOcrOnlySource ? "ocr-required" : "not-found");
    }

    private static string? ExtractValueAfterHeading(string text, string heading)
    {
        var headingPattern = string.Join("\\s+", Regex.Split(heading.Trim(), "\\s+").Select(Regex.Escape));
        var match = Regex.Match(text, $"{headingPattern}\\s*[:\\-]?\\s*(?<value>[^\\r\\n]{{2,160}})", RegexOptions.IgnoreCase);
        if (!match.Success) return null;
        var value = match.Groups["value"].Value.Trim(' ', ':', '-', '–');
        return value.Length < 2 ? null : value;
    }

    private static int? FindPageNumber(string text, string needle)
    {
        var position = text.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (position < 0) return null;
        var priorText = text[..position];
        var pages = Regex.Matches(priorText, "--- Page (?<page>\\d+) ---");
        return pages.Count == 0 ? null : int.Parse(pages[^1].Groups["page"].Value);
    }

    private static int FindHeaderRow(IXLWorksheet sheet)
    {
        foreach (var row in sheet.RowsUsed().Take(20))
        {
            if (FindColumn(sheet, row.RowNumber(), "heading") > 0) return row.RowNumber();
        }
        return 0;
    }

    private static int FindColumn(IXLWorksheet sheet, int rowNumber, string normalizedHeader)
    {
        if (rowNumber == 0) return 0;
        return sheet.Row(rowNumber).CellsUsed()
            .FirstOrDefault(cell => NormalizeHeader(cell.GetString()) == normalizedHeader)?.Address.ColumnNumber ?? 0;
    }

    private static string NormalizeHeader(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    public static string InferEntityType(string heading)
    {
        var normalized = heading.ToUpperInvariant();
        if (normalized.Contains("VESSEL")) return "V";
        if (normalized.Contains("PORT") || normalized.Contains("PLACE") || normalized.Contains("COUNTRY")) return "U";
        if (normalized.Contains("DIRECTOR") || normalized.Contains("SIGNATORY") || normalized.Contains("SIGNATURE")) return "I";
        return "O";
    }
}
