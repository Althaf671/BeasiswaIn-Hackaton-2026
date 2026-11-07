
 
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
}