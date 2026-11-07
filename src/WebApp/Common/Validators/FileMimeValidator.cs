using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Common.Errors;
using PdfSharp.Pdf.IO;
using SkiaSharp;

namespace BeasiswaIn.Web.Common.Validators;

internal static class FileMimeValidator
{
    public static async Task<Result> ValidateAsync(
        IFormFile? file,
        IEnumerable<string>? allowedMimeTypes = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0)
            return Result.Failure(FileMimeValidatorError.FileRequired);

        if (file.Length > FileValidatorConstant.MaxFileSize)
            return Result.Failure(FileMimeValidatorError.FileTooLarge);

        var contentType = file.ContentType.Trim();

        var allowed = new HashSet<string>(
            allowedMimeTypes ?? FileValidatorConstant.SupportedMimeTypes,
            StringComparer.OrdinalIgnoreCase);

        if (!allowed.Contains(contentType)
            || !FileValidatorConstant.SupportedMimeTypes.Contains(contentType))
            return Result.Failure(FileMimeValidatorError.UnsupportedType);

        var buffer = new byte[(int)file.Length];

        await using var stream = file.OpenReadStream();
        await stream.ReadExactlyAsync(buffer.AsMemory(), cancellationToken);

        var detectedMime = DetectMimeType(buffer);

        if (!string.Equals(
            detectedMime,
            NormalizeMime(contentType),
            StringComparison.OrdinalIgnoreCase))
            return Result.Failure(FileMimeValidatorError.ContentMismatch);

        try
        {
            return detectedMime switch
            {
                "image/jpeg" or "image/png" or "image/webp" => ValidateImage(buffer),
                "application/pdf" => ValidatePdf(buffer),
                _ => Result.Failure(FileMimeValidatorError.UnsupportedType)
            };
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException
            && ex is not OutOfMemoryException)
        {
            return Result.Failure(FileMimeValidatorError.ProcessingFailed);
        }
    }

    private static Result ValidateImage(byte[] buffer)
    {
        using var data = SKData.CreateCopy(buffer);

        using var codec = SKCodec.Create(data);
        if (codec is null)
            return Result.Failure(FileMimeValidatorError.InvalidImage);

        var info = codec.Info;

        if (info.Width <= 0 || info.Height <= 0)
            return Result.Failure(FileMimeValidatorError.InvalidImage);

        if ((long)info.Width * info.Height > FileValidatorConstant.MaxPixelCount)
            return Result.Failure(FileMimeValidatorError.ImageTooLarge);

        using var bitmap = SKBitmap.Decode(codec);

        if (bitmap is null)
            return Result.Failure(FileMimeValidatorError.InvalidImage);

        return Result.Success;
    }

    private static Result ValidatePdf(byte[] buffer)
    {
        try
        {
            using var stream = new MemoryStream(buffer, writable: false);
            using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

            if (document.PageCount <= 0)
                return Result.Failure(FileMimeValidatorError.PdfEmpty);

            if (document.PageCount > FileValidatorConstant.MaxPdfPages)
                return Result.Failure(FileMimeValidatorError.PdfTooManyPages);

            return Result.Success;
        }
        catch (Exception ex) when (ex is PdfReaderException or InvalidOperationException)
        {
            return Result.Failure(FileMimeValidatorError.InvalidPdf);
        }
    }

    private static string? DetectMimeType(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith(FileValidatorConstant.JpegSignature)) 
            return "image/jpeg";

        if (data.StartsWith(FileValidatorConstant.PngSignature)) 
            return "image/png";

        if (data.StartsWith(FileValidatorConstant.PdfSignature)) 
            return "application/pdf";

        if (data.Length >= 12
            && data.StartsWith(FileValidatorConstant.RiffSignature)
            && data[8..12].SequenceEqual(FileValidatorConstant.WebpSignature))
            return "image/webp";

        return null;
    }

    private static string NormalizeMime(string mime) =>
        mime.Equals("image/jpg", StringComparison.OrdinalIgnoreCase) 
            ? "image/jpeg" 
            : mime;
}