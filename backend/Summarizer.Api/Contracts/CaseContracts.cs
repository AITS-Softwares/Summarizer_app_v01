namespace Summarizer.Api.Contracts;

public sealed record ClientResponse(Guid Id, string Name, string ReferenceCode, bool IsActive);
public sealed record CaseResponse(Guid Id, Guid ClientId, string ClientName, string Name, string Status, DateTime CreatedAtUtc, IReadOnlyCollection<string> Permissions);
public sealed record CreateClientRequest(string Name, string ReferenceCode);
public sealed record CreateCaseRequest(Guid ClientId, string Name);
public sealed record GrantCaseAccessRequest(string UserId, IReadOnlyCollection<string> Permissions);
public sealed record UserOptionResponse(string Id, string DisplayName, string UserName, bool IsActive);
