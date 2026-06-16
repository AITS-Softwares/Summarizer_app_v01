using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Models;
using Summarizer.Api.Services;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(
    AppDbContext dbContext,
    DocumentTextExtractor textExtractor,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyCollection<LibraryDocumentResponse>> GetAll(CancellationToken cancellationToken)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .OrderByDescending(item => item.UploadedAtUtc)
            .Select(item => new LibraryDocumentResponse(
                item.Id,
                item.OriginalName,
                item.ContentType,
                item.SizeBytes,
                item.Status,
                item.UploadedAtUtc,
                item.ConversationId,
                item.Conversation.Title))
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<IReadOnlyCollection<DocumentResponse>>> Upload(
        [FromForm] Guid conversationId,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0) return BadRequest("At least one file is required.");

        var conversation = await dbContext.Conversations.FindAsync([conversationId], cancellationToken);
        if (conversation is null) return NotFound("Conversation not found.");

        var configuredPath = configuration["Storage:FilesPath"] ?? "../../storage/files";
        var storageRoot = Path.GetFullPath(configuredPath, environment.ContentRootPath);
        var conversationPath = Path.Combine(storageRoot, conversationId.ToString("N"));
        Directory.CreateDirectory(conversationPath);

        var added = new List<StoredDocument>();
        foreach (var file in files.Where(file => file.Length > 0))
        {
            var safeOriginalName = Path.GetFileName(file.FileName);
            var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(safeOriginalName)}";
            var storagePath = Path.Combine(conversationPath, storedName);

            await using (var stream = System.IO.File.Create(storagePath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var extraction = await textExtractor.ExtractAsync(storagePath, safeOriginalName, cancellationToken);
            var document = new StoredDocument
            {
                ConversationId = conversationId,
                OriginalName = safeOriginalName,
                StoredName = storedName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                SizeBytes = file.Length,
                StoragePath = storagePath,
                ExtractedText = extraction.Text,
                Status = extraction.Status
            };
            added.Add(document);
            dbContext.Documents.Add(document);
        }

        conversation.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(added.Select(document => new DocumentResponse(
            document.Id,
            document.OriginalName,
            document.ContentType,
            document.SizeBytes,
            document.Status,
            document.UploadedAtUtc)));
    }

    [HttpPost("{id:guid}/reprocess")]
    public async Task<ActionResult<DocumentResponse>> Reprocess(Guid id, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null) return NotFound();
        if (!System.IO.File.Exists(document.StoragePath))
        {
            document.Status = "missing";
            await dbContext.SaveChangesAsync(cancellationToken);
            return NotFound("The stored file could not be found.");
        }

        var extraction = await textExtractor.ExtractAsync(
            document.StoragePath,
            document.OriginalName,
            cancellationToken);
        document.ExtractedText = extraction.Text;
        document.Status = extraction.Status;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new DocumentResponse(
            document.Id,
            document.OriginalName,
            document.ContentType,
            document.SizeBytes,
            document.Status,
            document.UploadedAtUtc));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null) return NotFound();

        if (System.IO.File.Exists(document.StoragePath))
        {
            System.IO.File.Delete(document.StoragePath);
        }

        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
