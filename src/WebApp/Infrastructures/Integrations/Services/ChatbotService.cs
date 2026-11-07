using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Interfaces;

namespace BeasiswaIn.Web.Infrastructures.Integrations.Services;

internal sealed class ChatbotService : IChatbotService
{
    public async Task<Result<string>> SendChatCompletionAsync(
        string message, 
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}