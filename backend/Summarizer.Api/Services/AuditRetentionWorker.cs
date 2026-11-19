using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Data;

namespace Summarizer.Api.Services;

/// <summary>Deletes only audit entries whose configured retention period has elapsed.</summary>
public sealed class AuditRetentionWorker(IServiceScopeFactory scopeFactory, ILogger<AuditRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await PurgeAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken)) await PurgeAsync(stoppingToken);
    }

    private async Task PurgeAsync(CancellationToken token)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var policies = await db.AuditRetentionPolicies.AsNoTracking().Where(x => !x.KeepForever && x.RetentionDays != null).ToListAsync(token);
            foreach (var policy in policies)
            {
                var cutoff = DateTime.UtcNow.AddDays(-policy.RetentionDays!.Value);
                await db.AuditEvents.Where(x => x.Category == policy.Category && x.OccurredAtUtc < cutoff).ExecuteDeleteAsync(token);
            }
        }
        catch (Exception exception) when (!token.IsCancellationRequested)
        {
            logger.LogError(exception, "Audit retention cleanup failed.");
        }
    }
}
