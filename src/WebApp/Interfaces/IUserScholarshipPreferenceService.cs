using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Models.User;

namespace BeasiswaIn.Web.Interfaces;

public interface IUserScholarshipPreferenceService
{
    public Task SaveScholarshipPreferencesAsync(
        string userId,
        IReadOnlyCollection<string> categoryIds,
        CancellationToken cancellationToken);
}