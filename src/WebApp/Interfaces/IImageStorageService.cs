

using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Models.Dashboard;

namespace BeasiswaIn.Web.Interfaces;

public interface IImageStorageService
{
    public Task<Result<ImageUploadResponse>> UploadAsync(
        IFormFile file,
        string directory,
        CancellationToken cancellationToken);

    public Task<Result> DeleteAsync(
        string publicId,
        CancellationToken cancellationToken);
}