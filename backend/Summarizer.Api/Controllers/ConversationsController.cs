using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Models;
using Summarizer.Api.Services;
namespace Summarizer.Api.Controllers;
[ApiController, Route("api/conversations"), Authorize(Policy = "ProcessingAdministration")]
public sealed class ConversationsController(AppDbContext db, AuditService audit) : ControllerBase
{
 [HttpGet] public async Task<IReadOnlyCollection<ConversationSummary>> GetAll([FromQuery] bool archived=false, CancellationToken token=default) => await db.Conversations.AsNoTracking().Where(x=>x.IsArchived==archived).OrderByDescending(x=>x.UpdatedAtUtc).Select(x=>new ConversationSummary(x.Id,x.Title,x.IsArchived,x.Messages.Count,x.Documents.Count,x.UpdatedAtUtc)).ToListAsync(token);
 [HttpGet("{id:guid}")] public async Task<ActionResult<ConversationDetails>> Get(Guid id,CancellationToken token) { var item=await db.Conversations.AsNoTracking().Include(x=>x.Messages.OrderBy(m=>m.CreatedAtUtc)).Include(x=>x.Documents.Where(d=>!d.IsArchived).OrderBy(d=>d.UploadedAtUtc)).SingleOrDefaultAsync(x=>x.Id==id,token); return item is null?NotFound():Ok(ToDetails(item)); }
 [HttpPost] public async Task<ActionResult<ConversationDetails>> Create(CreateConversationRequest request,CancellationToken token) { var item=new Conversation {Title=string.IsNullOrWhiteSpace(request.Title)?"New document analysis":request.Title.Trim(),CreatedByUserId=User.FindFirstValue(ClaimTypes.NameIdentifier)}; db.Conversations.Add(item); await db.SaveChangesAsync(token); await audit.RecordAsync("conversation.created",targetType:"conversation",targetId:item.Id.ToString(),cancellationToken:token); return CreatedAtAction(nameof(Get),new{id=item.Id},ToDetails(item)); }
 [HttpPatch("{id:guid}/archive")] public async Task<IActionResult> Archive(Guid id,[FromQuery] bool archived=true,CancellationToken token=default) { var item=await db.Conversations.FindAsync([id],token); if(item is null)return NotFound();item.IsArchived=archived;item.UpdatedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync(token);await audit.RecordAsync(archived?"conversation.archived":"conversation.restored",targetType:"conversation",targetId:id.ToString(),cancellationToken:token);return NoContent(); }
 [HttpDelete("{id:guid}")] public Task<IActionResult> Delete(Guid id,CancellationToken token)=>Archive(id,true,token);
 private static ConversationDetails ToDetails(Conversation x)=>new(x.Id,x.Title,x.IsArchived,x.CreatedAtUtc,x.UpdatedAtUtc,x.Messages.Select(m=>new MessageResponse(m.Id,m.Role,m.Content,m.Provider,m.CreatedAtUtc)).ToArray(),x.Documents.Select(d=>new DocumentResponse(d.Id,d.OriginalName,d.ContentType,d.SizeBytes,d.Status,d.UploadedAtUtc)).ToArray());
}
