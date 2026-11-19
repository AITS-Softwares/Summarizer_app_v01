namespace Summarizer.Api.Contracts;

public sealed record LoginRequest(string UserNameOrEmail, string Password, bool RememberMe = false);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record CurrentUserResponse(string Id, string UserName, string DisplayName, string Email, IReadOnlyCollection<string> Roles, bool MustChangePassword);
public sealed record CreateUserRequest(string UserName, string Email, string DisplayName, string TemporaryPassword, IReadOnlyCollection<string> Roles);
public sealed record UpdateUserRolesRequest(IReadOnlyCollection<string> Roles);
public sealed record DisableUserRequest(string? Reason);
public sealed record ManagedUserResponse(string Id, string UserName, string DisplayName, string Email, bool IsActive, bool MustChangePassword, DateTime CreatedAtUtc, DateTime? LastLoginAtUtc, IReadOnlyCollection<string> Roles);
public sealed record AuditEventResponse(Guid Id, DateTime OccurredAtUtc, string Category, string EventType, string? ActorUserId, string? ActorUserName, string? ActorRoles, string? TargetType, string? TargetId, string Outcome, string? Detail, string? CorrelationId);
public sealed record AuditRetentionPolicyResponse(string Category, bool KeepForever, int? RetentionDays, DateTime UpdatedAtUtc, string? UpdatedByUserId);
public sealed record UpdateAuditRetentionPolicyRequest(string Category, bool KeepForever, int? RetentionDays);
