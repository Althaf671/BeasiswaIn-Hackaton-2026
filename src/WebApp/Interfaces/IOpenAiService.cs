using BeasiswaIn.Web.Models.Chatbot;
using BeasiswaIn.Web.Models.Scholarships;
using OpenAI.Chat;

namespace BeasiswaIn.Web.Interfaces;

public interface IOpenAiService
{
    public Task<string> ExtractCertificateAsync(
        string certificateUrl,
        CancellationToken cancellationToken);

    public Task<string> AnalyzeBestScholarshipChoices(
        string essay,
        string extractionResult,
        IReadOnlyList<string> categoryTags,
        IReadOnlyList<ScholarshipResponse> scholarships,
        CancellationToken cancellationToken);

    public Task<string> SendMessageAsync(
        string message,
        List<ChatMessage> conversation,
        CancellationToken cancellationToken);
}