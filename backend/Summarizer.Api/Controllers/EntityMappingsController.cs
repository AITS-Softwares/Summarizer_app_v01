using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExcelDataReader;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Models;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/entity-mappings")]
public sealed class EntityMappingsController(AppDbContext dbContext) : ControllerBase
{
    private const long MaximumImportSizeBytes = 10 * 1024 * 1024;

    [HttpGet]
    public async Task<IReadOnlyCollection<EntityMappingResponse>> GetAll(CancellationToken cancellationToken)
    {
        return await dbContext.EntityMappings
            .AsNoTracking()
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Heading)
            .ThenBy(item => item.Priority)
            .ThenBy(item => item.EntityName)
            .Select(item => ToResponse(item))
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<EntityMappingResponse>> Create(
        SaveEntityMappingRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null) return BadRequest(validationError);

        var mapping = new EntityMapping();
        Apply(mapping, request);
        dbContext.EntityMappings.Add(mapping);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = mapping.Id }, ToResponse(mapping));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EntityMappingResponse>> Update(
        Guid id,
        SaveEntityMappingRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null) return BadRequest(validationError);

        var mapping = await dbContext.EntityMappings.FindAsync([id], cancellationToken);
        if (mapping is null) return NotFound();

        Apply(mapping, request);
        mapping.Version++;
        mapping.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(mapping));
    }

    [HttpGet("import-template")]
    public IActionResult DownloadImportTemplate()
    {
        const string csv = "Heading,Name*,Alternate Name(s),Case ID,Entity Type*,Gender,Date of Birth,Country Location,Place of Birth,Citizenship,Registered Country,IMO Number,Identification Number(s)\r\nBeneficiary,PT ACINTYA GLOBAL LOGISTIK,,,O,,,,,,,,\r\n";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "entity-master-import-template.csv");
    }

    [HttpPost("bulk-import")]
    [RequestSizeLimit(MaximumImportSizeBytes)]
    public async Task<ActionResult<EntityMappingImportResponse>> BulkImport(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest("Choose a CSV, XLS, or XLSX file to import.");
        if (file.Length > MaximumImportSizeBytes) return BadRequest("The import file must be 10 MB or smaller.");

        var extension = Path.GetExtension(file.FileName);
        if (!new[] { ".csv", ".xls", ".xlsx" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Only CSV, XLS, and XLSX files are supported.");
        }

        List<ImportedMapping> importedRows;
        try
        {
            await using var stream = file.OpenReadStream();
            importedRows = ReadImportedRows(stream, extension);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or HeaderException)
        {
            return BadRequest(exception.Message);
        }

        var existing = await dbContext.EntityMappings.ToListAsync(cancellationToken);
        var existingByKey = existing
            .GroupBy(item => ToKey(item.Heading, item.EntityName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.Version).First(), StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in importedRows)
        {
            if (string.IsNullOrWhiteSpace(row.Heading) && string.IsNullOrWhiteSpace(row.EntityName) && string.IsNullOrWhiteSpace(row.EntityTypeCode)) continue;
            if (string.IsNullOrWhiteSpace(row.Heading) || string.IsNullOrWhiteSpace(row.EntityName) || string.IsNullOrWhiteSpace(row.EntityTypeCode))
            {
                skipped++;
                errors.Add($"Row {row.RowNumber}: Heading, Name*, and Entity Type* are required.");
                continue;
            }

            var key = ToKey(row.Heading, row.EntityName);
            if (existingByKey.TryGetValue(key, out var mapping))
            {
                mapping.EntityTypeCode = row.EntityTypeCode;
                mapping.Description = row.Description;
                mapping.Priority = row.Priority;
                mapping.IsActive = row.IsActive;
                mapping.Version++;
                mapping.UpdatedAtUtc = DateTime.UtcNow;
                updated++;
                continue;
            }

            mapping = new EntityMapping
            {
                Heading = row.Heading,
                EntityName = row.EntityName,
                EntityTypeCode = row.EntityTypeCode,
                Description = row.Description,
                Priority = row.Priority,
                IsActive = row.IsActive
            };
            dbContext.EntityMappings.Add(mapping);
            existingByKey[key] = mapping;
            created++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new EntityMappingImportResponse(created, updated, skipped, errors.Take(20).ToArray()));
    }

    private static string? Validate(SaveEntityMappingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Heading)) return "Heading is required.";
        if (string.IsNullOrWhiteSpace(request.EntityName)) return "Entity name is required.";
        if (string.IsNullOrWhiteSpace(request.EntityTypeCode)) return "Entity type code is required.";
        if (request.Priority is < 0 or > 10_000) return "Priority must be between 0 and 10,000.";
        return null;
    }

    private static void Apply(EntityMapping mapping, SaveEntityMappingRequest request)
    {
        mapping.Heading = request.Heading.Trim();
        mapping.EntityName = request.EntityName.Trim();
        mapping.EntityTypeCode = request.EntityTypeCode.Trim().ToUpperInvariant();
        mapping.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        mapping.Priority = request.Priority;
        mapping.IsActive = request.IsActive;
    }

    private static List<ImportedMapping> ReadImportedRows(Stream stream, string extension)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var reader = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? ExcelReaderFactory.CreateCsvReader(stream)
            : ExcelReaderFactory.CreateReader(stream);

        if (!reader.Read()) throw new HeaderException("The import file has no header row.");
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var column = 0; column < reader.FieldCount; column++)
        {
            var value = reader.GetValue(column)?.ToString();
            if (!string.IsNullOrWhiteSpace(value)) headers[NormalizeHeader(value)] = column;
        }

        if (!headers.ContainsKey("heading") || !headers.ContainsKey("name") || !headers.ContainsKey("entitytype"))
        {
            throw new HeaderException("The import needs the client template columns Heading, Name*, and Entity Type*.");
        }

        var rows = new List<ImportedMapping>();
        var rowNumber = 1;
        while (reader.Read())
        {
            rowNumber++;
            rows.Add(new ImportedMapping(
                rowNumber,
                ReadCell(reader, headers, "heading"),
                ReadCell(reader, headers, "name"),
                ReadCell(reader, headers, "entitytype").ToUpperInvariant(),
                ReadCell(reader, headers, "description", allowMissing: true),
                ParsePriority(ReadCell(reader, headers, "priority", allowMissing: true)),
                ParseActive(ReadCell(reader, headers, "status", allowMissing: true))));
        }

        return rows;
    }

    private static string ReadCell(IExcelDataReader reader, IReadOnlyDictionary<string, int> headers, string name, bool allowMissing = false)
    {
        if (!headers.TryGetValue(name, out var index)) return allowMissing ? string.Empty : throw new HeaderException($"Missing {name} column.");
        return reader.GetValue(index)?.ToString()?.Trim() ?? string.Empty;
    }

    private static int ParsePriority(string value) => int.TryParse(value, out var priority) ? Math.Clamp(priority, 0, 10_000) : 100;

    private static bool ParseActive(string value) => string.IsNullOrWhiteSpace(value)
        || value.Equals("active", StringComparison.OrdinalIgnoreCase)
        || value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
        || value == "1";

    private static string NormalizeHeader(string value) => new(value
        .Where(char.IsLetterOrDigit)
        .Select(char.ToLowerInvariant)
        .ToArray());

    private static string ToKey(string heading, string entityName) => $"{heading.Trim()}\u001f{entityName.Trim()}";

    private sealed record ImportedMapping(int RowNumber, string Heading, string EntityName, string EntityTypeCode, string? Description, int Priority, bool IsActive);
    private sealed class HeaderException(string message) : Exception(message);

    private static EntityMappingResponse ToResponse(EntityMapping mapping) => new(
        mapping.Id,
        mapping.Heading,
        mapping.EntityName,
        mapping.EntityTypeCode,
        mapping.Description,
        mapping.Priority,
        mapping.IsActive,
        mapping.Version,
        mapping.UpdatedAtUtc);
}
