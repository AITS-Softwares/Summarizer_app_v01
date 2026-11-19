using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Models;
using Summarizer.Api.Services;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(AppDbContext dbContext, UserManager<ApplicationUser> userManager, AuditService auditService) : ControllerBase
{
    private static readonly string[] AllowedRoles = ["SystemAdministrator", "UserAdministrator", "ProcessingAdministrator", "Analyst", "Reviewer", "Auditor", "ReadOnly"];

    [HttpGet]
    [Authorize(Policy = "UserAdministration")]
    public async Task<IReadOnlyCollection<ManagedUserResponse>> GetAll(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking().OrderBy(item => item.UserName).ToListAsync(cancellationToken);
        // UserManager and DbContext are scoped to this request. Resolve roles one at a time;
        // parallel calls would attempt concurrent operations on the same DbContext.
        var result = new List<ManagedUserResponse>(users.Count);
        foreach (var user in users) result.Add(await ToResponse(user));
        return result;
    }

    [HttpPost]
    [Authorize(Policy = "UserAdministration")]
    public async Task<ActionResult<ManagedUserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var roles = ValidateRoles(request.Roles);
        if (roles is null) return BadRequest("One or more roles are not supported.");
        if (!CanManageRoles(roles)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName)) return BadRequest("Username, email, and display name are required.");

        var user = new ApplicationUser { UserName = request.UserName.Trim(), Email = request.Email.Trim(), DisplayName = request.DisplayName.Trim(), EmailConfirmed = true, MustChangePassword = true };
        var create = await userManager.CreateAsync(user, request.TemporaryPassword);
        if (!create.Succeeded) return BadRequest(string.Join(" ", create.Errors.Select(item => item.Description)));
        var roleResult = await userManager.AddToRolesAsync(user, roles);
        if (!roleResult.Succeeded) return BadRequest(string.Join(" ", roleResult.Errors.Select(item => item.Description)));
        await auditService.RecordAsync("identity.user.created", targetType: "user", targetId: user.Id, detail: $"Roles: {string.Join(", ", roles)}", cancellationToken: cancellationToken);
        return CreatedAtAction(nameof(GetAll), await ToResponse(user));
    }

    [HttpPut("{id}/roles")]
    [Authorize(Policy = "UserAdministration")]
    public async Task<ActionResult<ManagedUserResponse>> UpdateRoles(string id, UpdateUserRolesRequest request, CancellationToken cancellationToken)
    {
        var roles = ValidateRoles(request.Roles);
        if (roles is null || roles.Count == 0) return BadRequest("Assign at least one valid role.");
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier) || !CanManageRoles(roles)) return Forbid();
        var existing = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, existing);
        var result = await userManager.AddToRolesAsync(user, roles);
        if (!result.Succeeded) return BadRequest(string.Join(" ", result.Errors.Select(item => item.Description)));
        await auditService.RecordAsync("identity.user.roles.changed", targetType: "user", targetId: user.Id, detail: $"Roles: {string.Join(", ", roles)}", cancellationToken: cancellationToken);
        return Ok(await ToResponse(user));
    }

    [HttpPost("{id}/disable")]
    [Authorize(Policy = "UserAdministration")]
    public async Task<ActionResult<ManagedUserResponse>> Disable(string id, DisableUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier)) return BadRequest("Administrators cannot disable their own account.");
        user.IsActive = false;
        user.DisabledReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        await userManager.UpdateSecurityStampAsync(user);
        await userManager.UpdateAsync(user);
        await auditService.RecordAsync("identity.user.disabled", targetType: "user", targetId: user.Id, detail: user.DisabledReason, cancellationToken: cancellationToken);
        return Ok(await ToResponse(user));
    }

    private async Task<ManagedUserResponse> ToResponse(ApplicationUser user) => new(user.Id, user.UserName ?? string.Empty, user.DisplayName, user.Email ?? string.Empty, user.IsActive, user.MustChangePassword, user.CreatedAtUtc, user.LastLoginAtUtc, (await userManager.GetRolesAsync(user)).ToArray());
    private static IReadOnlyCollection<string>? ValidateRoles(IReadOnlyCollection<string> roles) => roles.All(role => AllowedRoles.Contains(role, StringComparer.Ordinal)) ? roles.Distinct(StringComparer.Ordinal).ToArray() : null;
    private bool CanManageRoles(IReadOnlyCollection<string> roles) => User.IsInRole("SystemAdministrator") || roles.All(role => role is "Analyst" or "Reviewer" or "Auditor" or "ReadOnly");
}
