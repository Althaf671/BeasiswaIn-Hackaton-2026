using BeasiswaIn.Web.Models.ScholarBot;

namespace BeasiswaIn.Web.Interfaces;

internal interface IScholarBotTools
{
    Task<ScholarBotToolResult> ExecuteAsync(string name, string arguments, CancellationToken cancellationToken);
}
