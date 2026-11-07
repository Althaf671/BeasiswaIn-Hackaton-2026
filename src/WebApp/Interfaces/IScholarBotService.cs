using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Models.ScholarBot;

namespace BeasiswaIn.Web.Interfaces;

public interface IScholarBotService
{
    Task<Result<ScholarBotResponse>> ChatAsync(ScholarBotRequest request, CancellationToken cancellationToken);
}
