using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Models;
using Summarizer.Api.Services;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/screening")]
[Authorize(Policy = "ProcessingAdministration")]
public sealed class ScreeningController(AppDbContext db, DocumentTextExtractor textExtractor, ScreeningProcessorService processor, VisionScreeningExtractor visionExtractor, IConfiguration config, IWebHostEnvironment environment, AuditService audit) : ControllerBase
{
    [HttpGet("templates")]
    public async Task<IReadOnlyCollection<ScreeningTemplateResponse>> GetTemplates(CancellationToken token) =>
        await db.ScreeningTemplates.AsNoTracking().OrderByDescending(x => x.IsActive).ThenByDescending(x => x.CreatedAtUtc).ToListAsync(token).ContinueWith(task => (IReadOnlyCollection<ScreeningTemplateResponse>)task.Result.Select(ToResponse).ToArray(), token);

    [HttpPost("templates")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ScreeningTemplateResponse>> RegisterTemplate([FromForm] string name, [FromForm] string version, [FromForm] IFormFile file, CancellationToken token)
    {
        if (file is null || file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) return BadRequest("Choose an XLSX template.");
        TemplateDefinition definition;
        try { await using var input = file.OpenReadStream(); definition = processor.ReadTemplate(input); }
        catch (Exception e) when (e is InvalidDataException or ArgumentException) { return BadRequest(e.Message); }
        var folder = StoragePath("templates"); Directory.CreateDirectory(folder); var path = Path.Combine(folder, $"{Guid.NewGuid():N}.xlsx");
        await using (var output = System.IO.File.Create(path)) await file.CopyToAsync(output, token);
        foreach (var item in await db.ScreeningTemplates.Where(x => x.IsActive).ToListAsync(token)) item.IsActive = false;
        var template = new ScreeningTemplate { Name = string.IsNullOrWhiteSpace(name) ? "World Check" : name.Trim(), Version = string.IsNullOrWhiteSpace(version) ? "v1" : version.Trim(), OriginalName = Path.GetFileName(file.FileName), StoragePath = path, WorksheetName = definition.WorksheetName, HeaderRowNumber = definition.HeaderRowNumber, RowsJson = ScreeningProcessorService.SerializeRows(definition.Rows) };
        db.ScreeningTemplates.Add(template); await db.SaveChangesAsync(token);
        await audit.RecordAsync("template.registered", targetType: "screening-template", targetId: template.Id.ToString(), detail: $"{template.Name} {template.Version}", cancellationToken: token);
        return Ok(ToResponse(template));
    }

    [HttpPost("runs")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<ScreeningRunResponse>> CreateRun([FromForm] Guid templateId, [FromForm] List<IFormFile> files, CancellationToken token)
    {
        var uploaded = files.Where(x => x.Length > 0).ToArray();
        if (uploaded.Length == 0) return BadRequest("Attach at least one source file.");
        var template = await db.ScreeningTemplates.SingleOrDefaultAsync(x => x.Id == templateId && x.IsActive, token);
        if (template is null) return BadRequest("Choose the active processing template.");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var run = new ScreeningRun { TemplateId = template.Id, Template = template, CreatedByUserId = userId };
        var folder = StoragePath(Path.Combine("screening", "runs", run.Id.ToString("N"))); Directory.CreateDirectory(folder);
        var sources = new List<ScreeningSource>(); var scanned = new List<ScannedScreeningSource>(); var sourceNames = new List<string>();
        foreach (var (file, index) in uploaded.Select((file, index) => (file, index)))
        {
            var name = Path.GetFileName(file.FileName); var path = Path.Combine(folder, $"{Guid.NewGuid():N}{Path.GetExtension(name)}");
            await using (var output = System.IO.File.Create(path)) await file.CopyToAsync(output, token);
            var extraction = await textExtractor.ExtractAsync(path, name, token);
            run.SourceFiles.Add(new ProcessingSourceFile { SourceFileIndex = index, OriginalName = name, StoredName = Path.GetFileName(path), StoragePath = path, ContentType = file.ContentType, SizeBytes = file.Length, Sha256 = await HashAsync(path, token), ExtractionStatus = extraction.Status, UploadedByUserId = userId });
            sources.Add(new ScreeningSource(index, name, extraction.Text, extraction.Status)); sourceNames.Add(name);
            if (extraction.Status == "ocr-required" && Path.GetExtension(name).Equals(".pdf", StringComparison.OrdinalIgnoreCase)) scanned.Add(new ScannedScreeningSource(index, name, path));
        }
        var templateRows = ScreeningProcessorService.DeserializeRows(template.RowsJson); var suggestions = processor.SuggestRows(templateRows, sources).ToList(); string? message = null;
        foreach (var source in scanned)
        {
            var settings = await db.ProviderSettings.SingleAsync(x => x.Id == 1, token); var result = await visionExtractor.ExtractAsync(templateRows, source, settings, token);
            if (result.ConfigurationMessage is null) { suggestions.RemoveAll(x => x.SourceFileIndex == source.Index); suggestions.AddRange(result.Suggestions); } else message ??= result.ConfigurationMessage;
        }
        run.SourceFilesJson = JsonSerializer.Serialize(sourceNames);
        run.Rows = suggestions.Select(x => new ScreeningRow { TemplateRowNumber = x.TemplateRowNumber, SourceFileIndex = x.SourceFileIndex, Heading = x.Heading, EntityName = x.EntityName, EntityTypeCode = x.EntityTypeCode, ExtractedFieldsJson = JsonSerializer.Serialize(CreateFields(x.EntityName, x.EntityTypeCode)), SourceFileName = x.SourceFileName, SourcePageNumber = x.SourcePageNumber, Confidence = x.Confidence, Status = x.Status }).ToList();
        run.ProcessingMessage = message; run.Status = message is null ? "completed" : "completed-with-ocr-warning";
        db.ScreeningRuns.Add(run); await db.SaveChangesAsync(token);
        await audit.RecordAsync("screening.run.created", targetType: "screening-run", targetId: run.Id.ToString(), detail: $"Template: {template.Name}; sources: {sourceNames.Count}", cancellationToken: token);
        foreach (var source in run.SourceFiles) await audit.RecordAsync("screening.source.uploaded", targetType: "processing-source-file", targetId: source.Id.ToString(), detail: $"Run: {run.Id}; file: {source.OriginalName}; SHA-256: {source.Sha256}", cancellationToken: token);
        return Ok(ToResponse(run));
    }

    [HttpGet("runs")]
    public async Task<ActionResult<IReadOnlyCollection<ScreeningRunResponse>>> GetRuns(CancellationToken token)
    {
        var runs = await db.ScreeningRuns.AsNoTracking().Include(x => x.Template).Include(x => x.Rows).Include(x => x.SourceFiles).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(token);
        return Ok(runs.Select(ToResponse).ToArray());
    }
    [HttpGet("runs/{id:guid}")]
    public async Task<ActionResult<ScreeningRunResponse>> GetRun(Guid id, CancellationToken token)
    {
        var run = await db.ScreeningRuns.Include(x => x.Template).Include(x => x.Rows).Include(x => x.SourceFiles).SingleOrDefaultAsync(x => x.Id == id, token);
        return run is null ? NotFound() : Ok(ToResponse(run));
    }
    [HttpPatch("runs/{runId:guid}/rows/{rowId:guid}")]
    public async Task<ActionResult<ScreeningRunResponse>> UpdateRow(Guid runId, Guid rowId, UpdateScreeningRowRequest request, CancellationToken token)
    {
        var run = await db.ScreeningRuns.Include(x => x.Template).Include(x => x.Rows).Include(x => x.SourceFiles).SingleOrDefaultAsync(x => x.Id == runId, token); if (run is null) return NotFound();
        var row = run.Rows.SingleOrDefault(x => x.Id == rowId); if (row is null) return NotFound();
        row.EntityName = string.IsNullOrWhiteSpace(request.EntityName) ? null : request.EntityName.Trim(); row.EntityTypeCode = string.IsNullOrWhiteSpace(request.EntityTypeCode) ? null : request.EntityTypeCode.Trim().ToUpperInvariant(); row.ExtractedFieldsJson = JsonSerializer.Serialize(CreateFields(row.EntityName, row.EntityTypeCode)); row.Status = string.IsNullOrWhiteSpace(request.Status) ? "needs-review" : request.Status; row.UpdatedAtUtc = DateTime.UtcNow; run.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(token); await audit.RecordAsync("screening.row.updated", targetType: "screening-row", targetId: row.Id.ToString(), detail: $"Run: {run.Id}; status: {row.Status}", cancellationToken: token); return Ok(ToResponse(run));
    }
    [HttpPost("runs/{id:guid}/export")]
    public async Task<ActionResult> Export(Guid id, CancellationToken token)
    {
        var run = await db.ScreeningRuns.Include(x => x.Template).Include(x => x.Rows).SingleOrDefaultAsync(x => x.Id == id, token); if (run is null) return NotFound();
        var output = processor.ExportCompletedTemplate(run.Template, run.Rows.ToArray(), StoragePath("screening/exports")); run.OutputPath = output; run.Status = "completed"; run.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(token); await audit.RecordAsync("screening.run.downloaded", targetType: "screening-run", targetId: run.Id.ToString(), detail: run.Template.Name, cancellationToken: token); return PhysicalFile(output, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{run.Template.Name}-completed.xlsx");
    }
    private string StoragePath(string folder) => Path.GetFullPath(Path.Combine(config["Storage:FilesPath"] ?? "../../storage/files", folder), environment.ContentRootPath);
    private static Dictionary<string, string> CreateFields(string? name, string? type) { var fields = new Dictionary<string, string>(); if (!string.IsNullOrWhiteSpace(name)) fields["Name*"] = name; if (!string.IsNullOrWhiteSpace(type)) fields["Entity Type*"] = type; return fields; }
    private static ScreeningTemplateResponse ToResponse(ScreeningTemplate x) => new(x.Id, x.Name, x.Version, x.OriginalName, x.WorksheetName, ScreeningProcessorService.DeserializeRows(x.RowsJson).Count, x.IsActive, x.CreatedAtUtc);
    private static ScreeningRunResponse ToResponse(ScreeningRun x) => new(x.Id, x.TemplateId, x.Template.Name, x.Status, x.ProcessingMessage, x.SourceFiles.Count > 0 ? x.SourceFiles.OrderBy(s => s.SourceFileIndex).Select(s => s.OriginalName).ToArray() : JsonSerializer.Deserialize<List<string>>(x.SourceFilesJson) ?? [], x.CreatedAtUtc, x.CreatedByUserId, x.Rows.OrderBy(r => r.SourceFileIndex).ThenBy(r => r.TemplateRowNumber).Select(r => new ScreeningRowResponse(r.Id, r.TemplateRowNumber, r.SourceFileIndex, r.Heading, r.EntityName, r.EntityTypeCode, JsonSerializer.Deserialize<Dictionary<string, string>>(r.ExtractedFieldsJson) ?? [], r.SourceFileName, r.SourcePageNumber, r.Confidence, r.Status)).ToArray());
    private static async Task<string> HashAsync(string path, CancellationToken token) { await using var input = System.IO.File.OpenRead(path); return Convert.ToHexString(await SHA256.HashDataAsync(input, token)); }
}
