using BeasiswaIn.Web.Infrastructures.Persistance;
using BeasiswaIn.Web.Models.Scholarships;

namespace BeasiswaIn.Web.Interfaces;

public interface IDatasetReader
{
    public Task<IReadOnlyList<ScholarshipDatasetRow>> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}