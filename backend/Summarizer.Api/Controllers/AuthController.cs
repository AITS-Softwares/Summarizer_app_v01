using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Summarizer.Api.Contracts;
using Summarizer.Api.Models;
using Summarizer.Api.Services;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AuditService auditService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<CurrentUserResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var key = request.UserNameOrEmail.Trim();
        var user = await userManager.FindByNameAsync(key) ?? await userManager.FindByEmailAsync(key);
        if (user is null || !user.IsActive)
        {
            await auditService.RecordAsync("authentication.login", "denied", "user", null, "Unknown or disabled account", cancellationToken);
            return Unauthorized("Invalid username or password.");
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            await auditService.RecordAsync("authentication.login", "denied", "user", user.Id, result.IsLockedOut ? "Account locked" : "Invalid password", cancellationToken);
            return Unauthorized(result.IsLockedOut ? "This account is temporarily locked." : "Invalid username or password.");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        await auditService.RecordAsync("authentication.login", targetType: "user", targetId: user.Id, cancellationToken: cancellationToken);
        return Ok(await ToResponse(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await auditService.RecordAsync("authentication.logout", cancellationToken: cancellationToken);
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null || !user.IsActive ? Unauthorized() : Ok(await ToResponse(user));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<CurrentUserResponse>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest("Current password and new password are required.");
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null || !user.IsActive) return Unauthorized();
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);
        await auditService.RecordAsync("identity.password.changed", targetType: "user", targetId: user.Id, cancellationToken: cancellationToken);
        return Ok(await ToResponse(user));
    }

    private async Task<CurrentUserResponse> ToResponse(ApplicationUser user) => new(user.Id, user.UserName ?? string.Empty, user.DisplayName, user.Email ?? string.Empty, (await userManager.GetRolesAsync(user)).ToArray(), user.MustChangePassword);
}
