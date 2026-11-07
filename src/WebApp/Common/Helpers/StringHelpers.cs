

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace BeasiswaIn.Web.Common.Helpers;

internal static partial class StringHelper
{
    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex ProblemTitleRegex();
    
    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex SeparateRegex();

    [GeneratedRegex(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled)]
    private static partial Regex WordBoundaryRegex();
    private static readonly Regex WordBoundary = WordBoundaryRegex();

    private static readonly Regex HtmlBreakRegex = MyRegex();

    private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);

    // Bullet / penomoran di tengah teks ("- ", "• ", "1. ", "2) "):
    // harus di awal string atau tepat setelah . ; : ! ?
    private static readonly Regex InlineBulletRegex = MyRegex1();

    // Penanda di awal item: "- ", "• ", "1. ", "2) "
    private static readonly Regex LeadingMarkerRegex = MyRegex2();

    private static readonly Regex WhitespaceRegex = MyRegex3();

    internal static bool IsStringLengthOutOfRange(string field, int min, int max)
        => field.Length < min || field.Length > max;

    internal static string ToProblemTitle(this string code)
    {
        var cleanCode = code.Contains('.') ? code.Split('.').Last() : code;
        return ProblemTitleRegex().Replace(cleanCode, "$1 $2");
    }

    extension(string word)
    {
        internal string SeparateWord => ToProblemTitle(word);
        internal string ToPascalCase => string.Concat(
            word.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        internal string ToPascalCaseWithSpaces => string.Join(
            ' ',
            WordBoundary.Replace(word, " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => char.ToUpperInvariant(x[0]) + x[1..]));
        internal string ToKebabCase => ToKebabCaseValue(word);
        internal string Digits => string.Concat(word.Where(char.IsDigit));
    }

    private static string ToKebabCaseValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var withSeparators = SeparateRegex().Replace(value, "-");

        return withSeparators.ToLowerInvariant();
    }

    public static string Elipsis(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (maxLength <= 0) return "...";
        if (value.Length <= maxLength) return value;

        return value[..maxLength].TrimEnd() + "...";
    }

    public static bool IsSafeUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public static string ToSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var normalized = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        var lastDash = false;

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                sb.Append(c);
                lastDash = false;
            }
            else if (!lastDash && sb.Length > 0)
            {
                sb.Append('-');
                lastDash = true;
            }
        }

        return sb.ToString().TrimEnd('-');
    }

    public static List<string> ResolveList(IEnumerable<string?>? source)
    {
        var result = new List<string>();
        if (source is null)
            return result;

        // 1. Ratakan: buang tag HTML, decode entity, pecah per baris
        var lines = new List<string>();
        foreach (var raw in source)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var text = HtmlBreakRegex.Replace(raw, "\n");
            text = HtmlTagRegex.Replace(text, string.Empty);
            text = WebUtility.HtmlDecode(text);

            lines.AddRange(text.Split('\n',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        // 2. Sambung potongan kalimat: baris diawali huruf kecil, tanpa bullet,
        //    dan baris sebelumnya belum diakhiri tanda baca penutup
        var merged = new List<string>();
        foreach (var line in lines)
        {
            var isContinuation = merged.Count > 0
                && char.IsLower(line[0])
                && !LeadingMarkerRegex.IsMatch(line)
                && ".;!?".IndexOf(merged[^1][^1]) < 0;

            if (isContinuation)
                merged[^1] += " " + line;
            else
                merged.Add(line);
        }

        // 3. Pecah di tiap bullet, bersihkan, rapikan
        foreach (var block in merged)
        {
            foreach (var part in InlineBulletRegex.Split(block))
            {
                var item = LeadingMarkerRegex.Replace(part.Trim(), string.Empty);
                item = WhitespaceRegex.Replace(item, " ").Trim().TrimEnd(';', ',');

                if (item.Length < 2)
                    continue;

                item = char.ToUpper(item[0]) + item[1..];

                if (!result.Contains(item, StringComparer.OrdinalIgnoreCase))
                    result.Add(item);
            }
        }

        return result;
    }

    [GeneratedRegex(@"<\s*/?\s*(br|p|li|div)\s*/?\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-ID")]
    private static partial Regex MyRegex();
    [GeneratedRegex(@"(?<=^|[.;:!?])\s*(?:[-–—•●▪◦*]|\d{1,2}[.)])\s+(?=\S)", RegexOptions.Compiled)]
    private static partial Regex MyRegex1();
    [GeneratedRegex(@"^\s*(?:[-–—•●▪◦*]|\d{1,2}[.)])\s+", RegexOptions.Compiled)]
    private static partial Regex MyRegex2();
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MyRegex3();
}