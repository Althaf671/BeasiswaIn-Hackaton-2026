using Humanizer;

namespace BeasiswaIn.Web.Common.Constants;

internal static class FileValidatorConstant
{
    public const int MaxFileSize = 10 * 1024 * 1024;
    public const long MaxPixelCount = 25_000_000;
    public const int MaxPdfPages = 20;

    public static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];
    public static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    public static ReadOnlySpan<byte> RiffSignature => "RIFF"u8;
    public static ReadOnlySpan<byte> WebpSignature => "WEBP"u8;
    public static ReadOnlySpan<byte> PdfSignature => "%PDF-"u8;

    public static readonly HashSet<string> SupportedMimeTypes = 
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    ];
}