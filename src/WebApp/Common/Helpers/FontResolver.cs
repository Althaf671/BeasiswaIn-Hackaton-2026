

using PdfSharp.Fonts;

namespace BeasiswaIn.Web.Common.Helpers;

public class FileFontResolver : IFontResolver
{
    private const string Regular = "Inter_18pt-Regular";
    private const string Bold = "Inter_18pt-Bold";

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(isBold ? Bold : Regular);
    }

    public byte[]? GetFont(string faceName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Internal", "Assets", "Fonts", faceName + ".ttf");

        if (!File.Exists(path))
            throw new FileNotFoundException($"Font tidak ditemukan: {path}");

        var bytes = File.ReadAllBytes(path);
        // Console.WriteLine($"[Font] {faceName}: {bytes.Length} byte");
        return bytes;
    }
}