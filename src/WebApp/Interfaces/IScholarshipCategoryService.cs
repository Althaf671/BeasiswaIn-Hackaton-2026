using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Models.Scholarships;

namespace BeasiswaIn.Web.Interfaces;

public interface IScholarshipCategoryService
{
    public Task<Result<string>> CreateAsync(
        CreateScholarshipCategoryRequest request,
        CancellationToken cancellationToken); 

    public Task<Result> UpdateAsync(
        string id,
        UpdateScholarshipCategoryRequest request,
        CancellationToken cancellationToken);

    public Task<Result> DeleteAsync(
        string id,
        CancellationToken cancellationToken);

    public Task<Result<ScholarshipCategoryResponse>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken);

    public Task<Result<ScholarshipCategoryListResponse>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    public Task<Result<IReadOnlyList<ScholarshipCategoryOptions>>> GetOptionsAsync(
        CancellationToken cancellationToken);
}