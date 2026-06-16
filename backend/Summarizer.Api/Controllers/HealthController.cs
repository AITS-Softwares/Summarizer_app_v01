using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Data;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var databaseAvailable = await dbContext.Database.CanConnectAsync(cancellationToken);
        return Ok(new
        {
            status = databaseAvailable ? "healthy" : "degraded",
            database = databaseAvailable ? "connected" : "unavailable",
            utc = DateTime.UtcNow
        });
    }
}
