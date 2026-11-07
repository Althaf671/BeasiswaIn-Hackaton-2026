using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Models.User;

namespace BeasiswaIn.Web.Interfaces;

public interface IUserScholarshipPreferenceService
{
    public Task<bool> SaveScholarshipPreferencesAsync(
        string userId,
        List<RecommendedScholarship> categoryIds,
        CancellationToken cancellationToken);
}