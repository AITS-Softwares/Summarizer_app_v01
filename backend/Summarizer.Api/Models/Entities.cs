namespace Summarizer.Api.Models;

public sealed class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "New document analysis";
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ChatMessage> Messages { get; set; } = [];
    public ICollection<StoredDocument> Documents { get; set; } = [];
}

public sealed class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string Provider { get; set; } = "api";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class StoredDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public string OriginalName { get; set; } = string.Empty;
    public string StoredName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? ExtractedText { get; set; }
    public string Status { get; set; } = "ready";
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ProviderSettings
{
    public int Id { get; set; } = 1;
    public string ProviderMode { get; set; } = "api";
    public string ApiBaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiModel { get; set; } = "gpt-4.1-mini";
    public string? ApiKeyEncrypted { get; set; }
    public string LocalBaseUrl { get; set; } = "http://localhost:11434";
    public string LocalModel { get; set; } = "qwen3:8b";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EntityMapping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Heading { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityTypeCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
