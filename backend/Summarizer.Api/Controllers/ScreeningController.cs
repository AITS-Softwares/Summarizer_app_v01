using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Models;
using Summarizer.Api.Services;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/screening")]
public sealed class ScreeningController(
    AppDbContext dbContext,
    DocumentTextExtractor documentTextExtractor,
    ScreeningProcessorService screeningProcessor,
    VisionScreeningExtractor visionScreeningExtractor,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("templates")]
    public async Task<IReadOnlyCollection<ScreeningTemplateResponse>> GetTemplates(CancellationToken cancellationToken) =>
        await dbContext.ScreeningTemplates.AsNoTracking().OrderByDescending(item => item.IsActive).ThenByDescending(item => item.CreatedAtUtc)
            .Select(item => ToResponse(item)).ToListAsync(cancellationToken);

    [HttpPost("templates")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ScreeningTemplateResponse>> RegisterTemplate(
        [FromForm] string name,
        [FromForm] string version,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest("Choose an XLSX template.");
        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) return BadRequest("Only XLSX templates are supported.");

        TemplateDefinition definition;
        try
        {
            await using var stream = file.OpenReadStream();
            definition = screeningProcessor.ReadTemplate(stream);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return BadRequest(exception.Message);
        }

        var root = StoragePath("templates");
        Directory.CreateDirectory(root);
        var storagePath = Path.Combine(root, $"{Guid.NewGuid():N}.xlsx");
        await using (var destination = System.IO.File.Create(storagePath)) await file.CopyToAsync(destination, cancellationToken);

        var existingTemplates = await dbContext.ScreeningTemplates.Where(item => item.IsActive).ToListAsync(cancellationToken);
        foreach (var existing in existingTemplates) existing.IsActive = false;
        var template = new ScreeningTemplate
        {
            Name = string.IsNullOrWhiteSpace(name) ? "World Check" : name.Trim(),
            Version = string.IsNullOrWhiteSpace(version) ? "v1" : version.Trim(),
            OriginalName = Path.GetFileName(file.FileName),
            StoragePath = storagePath,
            WorksheetName = definition.WorksheetName,
            HeaderRowNumber = definition.HeaderRowNumber,
            RowsJson = ScreeningProcessorService.SerializeRows(definition.Rows)
        };
        dbContext.ScreeningTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(template));
    }

    [HttpPost("runs")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<ScreeningRunResponse>> CreateRun(
        [FromForm] Guid templateId,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0) return BadRequest("Attach at least one source file.");
        var template = await dbContext.ScreeningTemplates.SingleOrDefaultAsync(item => item.Id == templateId && item.IsActive, cancellationToken);
        if (template is null) return BadRequest("Choose the active processing template.");

        var run = new ScreeningRun { TemplateId = template.Id, Template = template };
        var runDirectory = StoragePath(Path.Combine("screening", "runs", run.Id.ToString("N")));
        Directory.CreateDirectory(runDirectory);
        var sources = new List<ScreeningSource>();
        var scannedSources = new List<ScannedScreeningSource>();
        var sourceNames = new List<string>();
        foreach (var file in files.Where(item => item.Length > 0))
        {
            var originalName = Path.GetFileName(file.FileName);
            var storagePath = Path.Combine(runDirectory, $"{Guid.NewGuid():N}{Path.GetExtension(originalName)}");
            await using (var destination = System.IO.File.Create(storagePath)) await file.CopyToAsync(destination, cancellationToken);
            var extraction = await documentTextExtractor.ExtractAsync(storagePath, originalName, cancellationToken);
            sources.Add(new ScreeningSource(originalName, extraction.Text, extraction.Status));
            if (extraction.Status == "ocr-required" && Path.GetExtension(originalName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                scannedSources.Add(new ScannedScreeningSource(originalName, storagePath));
            }
            sourceNames.Add(originalName);
        }

        var templateRows = ScreeningProcessorService.DeserializeRows(template.RowsJson);
        var suggestions = screeningProcessor.SuggestRows(templateRows, sources);
        string? processingMessage = null;
        if (scannedSources.Count > 0)
        {
            var settings = await dbContext.ProviderSettings.SingleAsync(item => item.Id == 1, cancellationToken);
            var visionOutcome = await visionScreeningExtractor.ExtractAsync(templateRows, scannedSources, settings, cancellationToken);
            if (visionOutcome.ConfigurationMessage is null)
            {
                suggestions = visionOutcome.Suggestions;
            }
            else
            {
                processingMessage = visionOutcome.ConfigurationMessage;
            }
        }
        run.SourceFilesJson = JsonSerializer.Serialize(sourceNames);
        run.Rows = suggestions.Select(item =>
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(item.EntityName)) fields["Name*"] = item.EntityName;
            if (!string.IsNullOrWhiteSpace(item.EntityTypeCode)) fields["Entity Type*"] = item.EntityTypeCode;
            return new ScreeningRow
            {
                TemplateRowNumber = item.TemplateRowNumber,
                Heading = item.Heading,
                EntityName = item.EntityName,
                EntityTypeCode = item.EntityTypeCode,
                ExtractedFieldsJson = JsonSerializer.Serialize(fields),
            SourceFileName = item.SourceFileName,
            SourcePageNumber = item.SourcePageNumber,
            Confidence = item.Confidence,
                Status = item.Status
            };
        }).ToList();
        run.ProcessingMessage = processingMessage;
        run.Status = processingMessage is not null
            ? "ocr-provider-unavailable"
            : run.Rows.All(item => item.Status == "matched") ? "ready-to-export" : "review-required";
        dbContext.ScreeningRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(run));
    }

    [HttpGet("runs/{id:guid}")]
    public async Task<ActionResult<ScreeningRunResponse>> GetRun(Guid id, CancellationToken cancellationToken)
    {
        var run = await dbContext.ScreeningRuns.Include(item => item.Template).Include(item => item.Rows).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return run is null ? NotFound() : Ok(ToResponse(run));
    }

    [HttpPatch("runs/{runId:guid}/rows/{rowId:guid}")]
    public async Task<ActionResult<ScreeningRunResponse>> UpdateRow(Guid runId, Guid rowId, UpdateScreeningRowRequest request, CancellationToken cancellationToken)
    {
        var run = await dbContext.ScreeningRuns.Include(item => item.Template).Include(item => item.Rows).SingleOrDefaultAsync(item => item.Id == runId, cancellationToken);
        if (run is null) return NotFound();
        var row = run.Rows.SingleOrDefault(item => item.Id == rowId);
        if (row is null) return NotFound();
        row.EntityName = string.IsNullOrWhiteSpace(request.EntityName) ? null : request.EntityName.Trim();
        row.EntityTypeCode = string.IsNullOrWhiteSpace(request.EntityTypeCode) ? null : request.EntityTypeCode.Trim().ToUpperInvariant();
        var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(row.ExtractedFieldsJson)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(row.EntityName)) fields.Remove("Name*");
        else fields["Name*"] = row.EntityName;
        if (string.IsNullOrWhiteSpace(row.EntityTypeCode)) fields.Remove("Entity Type*");
        else fields["Entity Type*"] = row.EntityTypeCode;
        row.ExtractedFieldsJson = JsonSerializer.Serialize(fields);
        row.Status = string.IsNullOrWhiteSpace(request.Status) ? "needs-review" : request.Status;
        row.UpdatedAtUtc = DateTime.UtcNow;
        run.Status = run.Rows.All(item => item.Status is "matched" or "reviewed") ? "ready-to-export" : "review-required";
        run.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(run));
    }

    [HttpPost("runs/{id:guid}/approve-ready")]
    public async Task<ActionResult<ScreeningRunResponse>> ApproveReadyRows(Guid id, CancellationToken cancellationToken)
    {
        var run = await dbContext.ScreeningRuns.Include(item => item.Template).Include(item => item.Rows).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (run is null) return NotFound();

        var now = DateTime.UtcNow;
        foreach (var row in run.Rows.Where(item =>
                     !string.IsNullOrWhiteSpace(item.EntityName) &&
                     !string.IsNullOrWhiteSpace(item.EntityTypeCode) &&
                     item.Status is not ("matched" or "reviewed")))
        {
            row.Status = "reviewed";
            row.UpdatedAtUtc = now;
        }

        run.Status = run.Rows.All(item => item.Status is "matched" or "reviewed") ? "ready-to-export" : "review-required";
        run.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(run));
    }

    [HttpPost("runs/{id:guid}/export")]
    public async Task<ActionResult> Export(Guid id, CancellationToken cancellationToken)
    {
        var run = await dbContext.ScreeningRuns.Include(item => item.Template).Include(item => item.Rows).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (run is null) return NotFound();
        if (run.Rows.Any(item => item.Status is not ("matched" or "reviewed"))) return BadRequest("Review every unmatched or OCR-required row before export.");
        var outputPath = screeningProcessor.ExportCompletedTemplate(run.Template, run.Rows.ToArray(), StoragePath("screening/exports"));
        run.OutputPath = outputPath;
        run.Status = "exported";
        run.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return PhysicalFile(outputPath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{run.Template.Name}-completed.xlsx");
    }

    private string StoragePath(string folder) => Path.GetFullPath(Path.Combine(configuration["Storage:FilesPath"] ?? "../../storage/files", folder), environment.ContentRootPath);

    private static ScreeningTemplateResponse ToResponse(ScreeningTemplate template) => new(template.Id, template.Name, template.Version, template.OriginalName, template.WorksheetName, ScreeningProcessorService.DeserializeRows(template.RowsJson).Count, template.IsActive, template.CreatedAtUtc);
    private static ScreeningRunResponse ToResponse(ScreeningRun run) => new(run.Id, run.TemplateId, run.Template.Name, run.Status, run.ProcessingMessage, JsonSerializer.Deserialize<List<string>>(run.SourceFilesJson) ?? [], run.CreatedAtUtc, run.Rows.OrderBy(item => item.TemplateRowNumber).Select(item => new ScreeningRowResponse(item.Id, item.TemplateRowNumber, item.Heading, item.EntityName, item.EntityTypeCode, JsonSerializer.Deserialize<Dictionary<string, string>>(item.ExtractedFieldsJson) ?? [], item.SourceFileName, item.SourcePageNumber, item.Confidence, item.Status)).ToArray());
}
