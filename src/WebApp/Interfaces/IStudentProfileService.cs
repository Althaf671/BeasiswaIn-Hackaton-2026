using BeasiswaIn.Web.Common.Abstractions;

namespace BeasiswaIn.Web.Interfaces;

public interface IStudentProfileService
{
    public Task<Result> CreateAsync();
    public Task<Result> GetByIdAsync();
    public Task<Result> UpdateAsync();
}