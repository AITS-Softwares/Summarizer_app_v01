namespace Summarizer.Api.Contracts;

public sealed record CreateConversationRequest(string? Title);
public sealed record ConversationSummary(
    Guid Id,
    string Title,
    bool IsArchived,
    int MessageCount,
    int DocumentCount,
    DateTime UpdatedAtUtc);
public sealed record MessageResponse(
    Guid Id,
    string Role,
    string Content,
    string Provider,
    DateTime CreatedAtUtc);
public sealed record DocumentResponse(
    Guid Id,
    string Name,
    string ContentType,
    long SizeBytes,
    string Status,
    DateTime UploadedAtUtc);
public sealed record LibraryDocumentResponse(
    Guid Id,
    string Name,
    string ContentType,
    long SizeBytes,
    string Status,
    DateTime UploadedAtUtc,
    Guid ConversationId,
    string ConversationTitle);
public sealed record ConversationDetails(
    Guid Id,
    string Title,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyCollection<MessageResponse> Messages,
    IReadOnlyCollection<DocumentResponse> Documents);
public sealed record AnalysisRequest(Guid? ConversationId, string Prompt, string? Provider, Guid? EditMessageId);
public sealed record AnalysisResponse(
    Guid ConversationId,
    MessageResponse UserMessage,
    MessageResponse AssistantMessage,
    bool RequiresConfiguration);
public sealed record ProviderSettingsResponse(
    string ProviderMode,
    string ApiBaseUrl,
    string ApiModel,
    bool HasApiKey,
    string ApiKeyType,
    string LocalBaseUrl,
    string LocalModel);
public sealed record UpdateProviderSettingsRequest(
    string ProviderMode,
    string ApiBaseUrl,
    string ApiModel,
    string? ApiKey,
    bool ClearApiKey,
    string LocalBaseUrl,
    string LocalModel);
public sealed record ProviderTestResponse(bool Success, string Message, string Provider, string Endpoint);
public sealed record EntityMappingResponse(
    Guid Id,
    string Heading,
    string EntityName,
    string EntityTypeCode,
    string? Description,
    int Priority,
    bool IsActive,
    int Version,
    DateTime UpdatedAtUtc);
public sealed record SaveEntityMappingRequest(
    string Heading,
    string EntityName,
    string EntityTypeCode,
    string? Description,
    int Priority,
    bool IsActive);
public sealed record EntityMappingImportResponse(int Created, int Updated, int Skipped, IReadOnlyCollection<string> Errors);
