using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Models;
using Summarizer.Api.Services;
namespace Summarizer.Api.Controllers;
[ApiController, Route("api/audit-retention"), Authorize(Policy="SystemAdministration")]
public sealed class AuditRetentionController(AppDbContext db, AuditService audit) : ControllerBase
{
 private static readonly string[] Categories=["authentication","identity","configuration","processing","audit-access"];
 [HttpGet] public async Task<IReadOnlyCollection<AuditRetentionPolicyResponse>> Get(CancellationToken token)=>await db.AuditRetentionPolicies.AsNoTracking().OrderBy(x=>x.Category).Select(x=>new AuditRetentionPolicyResponse(x.Category,x.KeepForever,x.RetentionDays,x.UpdatedAtUtc,x.UpdatedByUserId)).ToListAsync(token);
 [HttpPut] public async Task<ActionResult<IReadOnlyCollection<AuditRetentionPolicyResponse>>> Update(IReadOnlyCollection<UpdateAuditRetentionPolicyRequest> request,CancellationToken token){if(request.Count!=Categories.Length||request.Select(x=>x.Category).Distinct(StringComparer.Ordinal).Count()!=Categories.Length||request.Any(x=>!Categories.Contains(x.Category,StringComparer.Ordinal)||(!x.KeepForever&&(x.RetentionDays is null||x.RetentionDays<1||x.RetentionDays>36500))))return BadRequest("Provide each audit category once. A non-permanent policy needs 1 to 36,500 retention days.");var actor=User.FindFirstValue(ClaimTypes.NameIdentifier);foreach(var change in request){var item=await db.AuditRetentionPolicies.FindAsync([change.Category],token)??new AuditRetentionPolicy{Category=change.Category};item.KeepForever=change.KeepForever;item.RetentionDays=change.KeepForever?null:change.RetentionDays;item.UpdatedAtUtc=DateTime.UtcNow;item.UpdatedByUserId=actor;if(db.Entry(item).State==EntityState.Detached)db.AuditRetentionPolicies.Add(item);}await db.SaveChangesAsync(token);await audit.RecordAsync("configuration.audit-retention.updated",targetType:"audit-retention",detail:"Audit retention policies updated.",cancellationToken:token);return Ok(await Get(token));}
}
