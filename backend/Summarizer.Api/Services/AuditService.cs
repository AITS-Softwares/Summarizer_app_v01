using System.Security.Claims;
using Summarizer.Api.Data;
using Summarizer.Api.Models;

namespace Summarizer.Api.Services;

public sealed class AuditService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
{
    public async Task RecordAsync(string eventType, string outcome = "success", string? targetType = null, string? targetId = null, string? detail = null, CancellationToken cancellationToken = default)
    {
        var context = httpContextAccessor.HttpContext;
        dbContext.AuditEvents.Add(new AuditEvent
        {
            EventType = eventType,
            Category = CategoryFor(eventType),
            Outcome = outcome,
            TargetType = targetType,
            TargetId = targetId,
            Detail = detail,
            ActorUserId = context?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            ActorUserName = context?.User.Identity?.Name,
            ActorRoles = context is null ? null : string.Join(", ", context.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).OrderBy(role => role)),
            CorrelationId = context?.TraceIdentifier
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string CategoryFor(string eventType) =>
        eventType.StartsWith("authentication.", StringComparison.Ordinal) ? "authentication" :
        eventType.StartsWith("identity.", StringComparison.Ordinal) ? "identity" :
        eventType.StartsWith("configuration.", StringComparison.Ordinal) ? "configuration" :
        eventType.StartsWith("audit.", StringComparison.Ordinal) ? "audit-access" :
        "processing";
}
