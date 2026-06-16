using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Models;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/conversations")]
public sealed class ConversationsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyCollection<ConversationSummary>> GetAll(
        [FromQuery] bool archived = false,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Conversations
            .AsNoTracking()
            .Where(item => item.IsArchived == archived)
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Select(item => new ConversationSummary(
                item.Id,
                item.Title,
                item.IsArchived,
                item.Messages.Count,
                item.Documents.Count,
                item.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDetails>> Get(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .Include(item => item.Messages.OrderBy(message => message.CreatedAtUtc))
            .Include(item => item.Documents.OrderBy(document => document.UploadedAtUtc))
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return conversation is null ? NotFound() : Ok(ToDetails(conversation));
    }

    [HttpPost]
    public async Task<ActionResult<ConversationDetails>> Create(
        CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = new Conversation
        {
            Title = string.IsNullOrWhiteSpace(request.Title) ? "New document analysis" : request.Title.Trim()
        };
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = conversation.Id }, ToDetails(conversation));
    }

    [HttpPatch("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, [FromQuery] bool archived = true, CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations.FindAsync([id], cancellationToken);
        if (conversation is null) return NotFound();

        conversation.IsArchived = archived;
        conversation.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .Include(item => item.Documents)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (conversation is null) return NotFound();

        foreach (var document in conversation.Documents)
        {
            if (System.IO.File.Exists(document.StoragePath))
            {
                System.IO.File.Delete(document.StoragePath);
            }
        }

        dbContext.Conversations.Remove(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static ConversationDetails ToDetails(Conversation item) =>
        new(
            item.Id,
            item.Title,
            item.IsArchived,
            item.CreatedAtUtc,
            item.UpdatedAtUtc,
            item.Messages.Select(message => new MessageResponse(
                message.Id, message.Role, message.Content, message.Provider, message.CreatedAtUtc)).ToArray(),
            item.Documents.Select(document => new DocumentResponse(
                document.Id,
                document.OriginalName,
                document.ContentType,
                document.SizeBytes,
                document.Status,
                document.UploadedAtUtc)).ToArray());
}
