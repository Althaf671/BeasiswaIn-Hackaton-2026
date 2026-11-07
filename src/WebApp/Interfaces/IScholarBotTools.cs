using BeasiswaIn.Web.Models.ScholarBot;
using OpenAI.Chat;

namespace BeasiswaIn.Web.Interfaces;

internal interface IScholarBotTools
{
    IReadOnlyList<ChatTool> Definitions { get; }
    Task<ScholarBotToolResult> ExecuteAsync(string name, string arguments, CancellationToken cancellationToken);
}
