using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Dashboard;
using BeasiswaIn.Web.Common.Errors;

namespace BeasiswaIn.Web.Infrastructures.Integrations.Services;

internal sealed class ImageStorageService : IImageStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<ImageStorageService> _logger;

    public ImageStorageService(
        Cloudinary cloudinary,
        ILogger<ImageStorageService> logger)
    {
        _cloudinary = cloudinary;
        _logger = logger;
    }

    public async Task<Result<ImageUploadResponse>> UploadAsync(
        IFormFile file,
        string directory,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return Result<ImageUploadResponse>.Failure(ImageStorageError.FileRequired);
        }

        if (file.Length == 0)
        {
            return Result<ImageUploadResponse>.Failure(ImageStorageError.FileEmpty);
        }

        if (string.IsNullOrWhiteSpace(directory))
        {
            return Result<ImageUploadResponse>.Failure(ImageStorageError.DirectoryRequired);
        }

        try
        {
            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = directory
            };

            _logger.LogInformation(
                "Uploading gambar ke Cloudinary directory {Directory}.",
                directory);

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            if (result.Error is not null)
            {
                var error = ImageStorageError.UploadFailed;
                _logger.LogError("Cloudinary gambar upload gagal: {Code} - {Error}", error.Code, result.Error.Message);
                return Result<ImageUploadResponse>.Failure(error);
            }

            if (string.IsNullOrWhiteSpace(result.PublicId)
                || result.SecureUrl is null)
            {
                var error = ImageStorageError.InvalidUploadResponse;
                _logger.LogError("{Code} - {Message}", error.Code, error.Description);
                return Result<ImageUploadResponse>.Failure(error);
            }

            _logger.LogInformation("Image uploaded sukses. PublicId: {PublicId}.", result.PublicId);

            return Result<ImageUploadResponse>.Success(
                new ImageUploadResponse
                {
                    Url = result.SecureUrl.ToString(),
                    PublicId = result.PublicId
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An exception occurred while uploading an image to Cloudinary.");

            return Result<ImageUploadResponse>.Failure(ImageStorageError.UploadFailed);
        }
    }

    public async Task<Result> DeleteAsync(
        string publicId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return Result.Failure(ImageStorageError.PublicIdRequired);
        }

        try
        {
            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };

            var result = await _cloudinary.DestroyAsync(deleteParams);
            if (result.Result == "ok")
            {
                _logger.LogInformation(
                    "Cloudinary image deleted successfully. PublicId: {PublicId}.",
                    publicId);

                return Result.Success;
            }

            if (result.Result == "not found")
            {
                _logger.LogInformation(
                    "Cloudinary image was already absent. PublicId: {PublicId}.",
                    publicId);

                return Result.Success;
            }

            _logger.LogWarning(
                "Cloudinary image deletion failed. PublicId: {PublicId}, Result: {Result}.",
                publicId,
                result.Result);

            return Result.Failure(ImageStorageError.DeleteFailed);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An exception occurred while deleting an image from Cloudinary. PublicId: {PublicId}.",
                publicId);

            return Result.Failure(ImageStorageError.DeleteFailed);
        }
    }
}