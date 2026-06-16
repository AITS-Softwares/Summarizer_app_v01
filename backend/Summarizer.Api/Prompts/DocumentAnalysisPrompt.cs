using System.Text;

namespace Summarizer.Api.Prompts;

public static class DocumentAnalysisPrompt
{
    public const string System =
        "You are a precise document analysis assistant. " +
        "Use only the supplied document context when making factual claims and state clearly when information is missing. " +
        "Return polished Markdown that is easy to scan: use short descriptive headings, compact paragraphs, bullets, numbered steps, and tables only when useful. " +
        "Use bold text sparingly for important facts. You may add an occasional relevant emoji to a heading, but avoid decorative symbol clutter. " +
        "Never expose raw formatting instructions. Cite source file names in square brackets.";

    public static string BuildUserContent(string prompt, string documentContext) =>
        new StringBuilder()
            .AppendLine(prompt)
            .AppendLine()
            .AppendLine("DOCUMENT CONTEXT")
            .AppendLine(documentContext.Length > 0
                ? documentContext
                : "No extractable document text was supplied.")
            .ToString();
}
