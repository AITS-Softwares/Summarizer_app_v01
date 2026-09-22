using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summarizer.Api.Contracts;
using Summarizer.Api.Data;
using Summarizer.Api.Models;
using Summarizer.Api.Services;

namespace Summarizer.Api.Controllers;

[ApiController]
[Route("api/analysis")]
public sealed class AnalysisController(AppDbContext dbContext, AiProviderService aiProviderService, IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AnalysisResponse>> Analyze(
        AnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (configuration.GetValue("ClientWorkflow:RestrictGenericAnalysis", true))
        {
            return BadRequest("Generic chat analysis is disabled in client workflow mode. Use Template Processing instead.");
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest("Prompt is required.");
        }

        Conversation conversation;
        if (request.ConversationId.HasValue)
        {
            var existingConversation = await dbContext.Conversations
                .Include(item => item.Documents)
                .Include(item => item.Messages)
                .SingleOrDefaultAsync(item => item.Id == request.ConversationId.Value, cancellationToken);
            if (existingConversation is null)
            {
                return NotFound("Conversation not found.");
            }

            conversation = existingConversation;
        }
        else
        {
            conversation = new Conversation { Title = CreateTitle(request.Prompt) };
            dbContext.Conversations.Add(conversation);
        }

        var settings = await dbContext.ProviderSettings.SingleAsync(item => item.Id == 1, cancellationToken);
        var provider = request.Provider is "local" ? "local" : request.Provider is "api" ? "api" : settings.ProviderMode;
        ChatMessage userMessage;
        if (request.EditMessageId.HasValue)
        {
            var messageToEdit = conversation.Messages.SingleOrDefault(item =>
                item.Id == request.EditMessageId.Value && item.Role == "user");
            if (messageToEdit is null)
            {
                return BadRequest("The message selected for editing was not found.");
            }

            var laterMessages = conversation.Messages
                .Where(item => item.CreatedAtUtc > messageToEdit.CreatedAtUtc)
                .ToArray();
            dbContext.Messages.RemoveRange(laterMessages);
            userMessage = messageToEdit;
            userMessage.Content = request.Prompt.Trim();
            userMessage.Provider = provider;
        }
        else
        {
            userMessage = new ChatMessage
            {
                Conversation = conversation,
                Role = "user",
                Content = request.Prompt.Trim(),
                Provider = provider
            };
            dbContext.Messages.Add(userMessage);
        }

        var context = BuildDocumentContext(conversation.Documents);
        var result = await aiProviderService.AnalyzeAsync(settings, provider, request.Prompt.Trim(), context, cancellationToken);
        var assistantMessage = new ChatMessage
        {
            Conversation = conversation,
            Role = "assistant",
            Content = result.Content,
            Provider = provider
        };
        dbContext.Messages.Add(assistantMessage);

        conversation.UpdatedAtUtc = DateTime.UtcNow;
        if (conversation.Title == "New document analysis")
        {
            conversation.Title = CreateTitle(request.Prompt);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new AnalysisResponse(
            conversation.Id,
            ToResponse(userMessage),
            ToResponse(assistantMessage),
            result.RequiresConfiguration));
    }

    private static string BuildDocumentContext(IEnumerable<StoredDocument> documents)
    {
        const int maximumCharacters = 60_000;
        var builder = new StringBuilder();
        foreach (var document in documents.Where(item => !string.IsNullOrWhiteSpace(item.ExtractedText)))
        {
            if (builder.Length >= maximumCharacters) break;
            builder.AppendLine($"--- SOURCE: {document.OriginalName} ---");
            var remaining = maximumCharacters - builder.Length;
            var text = document.ExtractedText!;
            builder.AppendLine(text[..Math.Min(text.Length, remaining)]);
        }

        return builder.ToString();
    }

    private static string CreateTitle(string prompt)
    {
        var normalized = string.Join(' ', prompt.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= 70 ? normalized : $"{normalized[..67]}...";
    }

    private static MessageResponse ToResponse(ChatMessage message) =>
        new(message.Id, message.Role, message.Content, message.Provider, message.CreatedAtUtc);
}
