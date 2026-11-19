using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Services;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/audit-events")]
[Authorize(Policy = "AuditAccess")]
public sealed class AuditController(AppDbContext dbContext, AuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyCollection<AuditEventResponse>> Get([FromQuery] int take = 200, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);
        var events = await dbContext.AuditEvents.AsNoTracking()
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(take)
            .Select(item => new AuditEventResponse(item.Id, item.OccurredAtUtc, item.Category, item.EventType, item.ActorUserId, item.ActorUserName, item.ActorRoles, item.TargetType, item.TargetId, item.Outcome, item.Detail, item.CorrelationId))
            .ToListAsync(cancellationToken);
        await auditService.RecordAsync("audit.events.viewed", targetType: "audit-event-list", detail: $"Returned {events.Count} events", cancellationToken: cancellationToken);
        return events;
    }
}
