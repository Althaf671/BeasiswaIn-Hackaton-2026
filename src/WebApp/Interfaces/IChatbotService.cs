using BeasiswaIn.Web.Common.Abstractions;

namespace BeasiswaIn.Web.Interfaces;

public interface IChatbotService
{
    public Task<Result<string>> SendChatCompletionAsync(
        string message, 
        CancellationToken cancellationToken);
}