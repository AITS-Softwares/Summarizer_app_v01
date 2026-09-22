using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Models;

namespace Summarizer.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<StoredDocument> Documents => Set<StoredDocument>();
    public DbSet<ProviderSettings> ProviderSettings => Set<ProviderSettings>();
    public DbSet<EntityMapping> EntityMappings => Set<EntityMapping>();
    public DbSet<ScreeningTemplate> ScreeningTemplates => Set<ScreeningTemplate>();
    public DbSet<ScreeningRun> ScreeningRuns => Set<ScreeningRun>();
    public DbSet<ScreeningRow> ScreeningRows => Set<ScreeningRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.Property(item => item.Title).HasMaxLength(200);
            entity.HasMany(item => item.Messages)
                .WithOne(item => item.Conversation)
                .HasForeignKey(item => item.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(item => item.Documents)
                .WithOne(item => item.Conversation)
                .HasForeignKey(item => item.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(item => item.Role).HasMaxLength(20);
            entity.Property(item => item.Provider).HasMaxLength(30);
        });

        modelBuilder.Entity<StoredDocument>(entity =>
        {
            entity.Property(item => item.OriginalName).HasMaxLength(260);
            entity.Property(item => item.StoredName).HasMaxLength(260);
            entity.Property(item => item.ContentType).HasMaxLength(150);
            entity.Property(item => item.Status).HasMaxLength(30);
        });

        modelBuilder.Entity<ProviderSettings>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProviderMode).HasMaxLength(20);
            entity.Property(item => item.ApiBaseUrl).HasMaxLength(500);
            entity.Property(item => item.ApiModel).HasMaxLength(150);
            entity.Property(item => item.LocalBaseUrl).HasMaxLength(500);
            entity.Property(item => item.LocalModel).HasMaxLength(150);
            entity.HasData(new ProviderSettings
            {
                Id = 1,
                ProviderMode = "api",
                ApiBaseUrl = "https://api.openai.com/v1",
                ApiModel = "gpt-4.1-mini",
                LocalBaseUrl = "http://localhost:11434",
                LocalModel = "qwen3:8b",
                UpdatedAtUtc = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        modelBuilder.Entity<EntityMapping>(entity =>
        {
            entity.Property(item => item.Heading).HasMaxLength(120);
            entity.Property(item => item.EntityName).HasMaxLength(300);
            entity.Property(item => item.EntityTypeCode).HasMaxLength(20);
            entity.Property(item => item.Description).HasMaxLength(500);
            entity.HasIndex(item => new { item.IsActive, item.Heading, item.EntityName });
        });

        modelBuilder.Entity<ScreeningTemplate>(entity =>
        {
            entity.Property(item => item.Name).HasMaxLength(150);
            entity.Property(item => item.Version).HasMaxLength(50);
            entity.Property(item => item.OriginalName).HasMaxLength(260);
            entity.HasIndex(item => item.IsActive);
        });

        modelBuilder.Entity<ScreeningRun>(entity =>
        {
            entity.Property(item => item.Status).HasMaxLength(40);
            entity.Property(item => item.ProcessingMessage).HasMaxLength(1000);
            entity.HasOne(item => item.Template)
                .WithMany()
                .HasForeignKey(item => item.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScreeningRow>(entity =>
        {
            entity.Property(item => item.Heading).HasMaxLength(120);
            entity.Property(item => item.EntityName).HasMaxLength(300);
            entity.Property(item => item.EntityTypeCode).HasMaxLength(20);
            entity.Property(item => item.ExtractedFieldsJson).HasColumnType("nvarchar(max)");
            entity.Property(item => item.SourceFileName).HasMaxLength(260);
            entity.Property(item => item.Status).HasMaxLength(40);
            entity.Property(item => item.Confidence).HasPrecision(5, 4);
            entity.HasIndex(item => new { item.ScreeningRunId, item.TemplateRowNumber }).IsUnique();
            entity.HasOne(item => item.ScreeningRun)
                .WithMany(item => item.Rows)
                .HasForeignKey(item => item.ScreeningRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
