using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Models.Scholarships;

namespace BeasiswaIn.Web.Interfaces;

public interface IScholarshipService
{
    public Task<Result<MetricScholarshipResponse>> GetMetricAsync(
        CancellationToken cancellationToken);

    public Task<Result<ScholarshipResponse>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken);

    public Task<Result<ScholarshipListResponse>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    public Task<Result<ScholarshipListResponse>> GetMyRecommendedAsync(
        string appUserId,
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);

    public Task<Result<ScholarshipListResponse>> GetLatestAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);

    public Task<Result<ScholarshipListResponse>> GetAllTrendingAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);

    public Task<Result<ScholarshipListResponse>> GetAllByCategoryId(
        string categoryId, 
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    public Task<Result<string>> CreateAsync(
        CreateScholarshipRequest request,
        CancellationToken cancellationToken);

    public Task<Result> UpdateAsync(
        string id,
        UpdateScholarshipRequest request,
        CancellationToken cancellationToken);

    public Task<Result> DeleteAsync(
        string id,
        CancellationToken cancellationToken);

    public Task<Result<IReadOnlyList<ScholarshipResponse>>> GetEligibleByCategoryIdsAsync(
        IReadOnlyCollection<string> categoryIds,
        CancellationToken cancellationToken);
}