

using System.Text.RegularExpressions;

namespace BeasiswaIn.Web.Common.Constants;

internal static partial class EmailConstant
{
    internal const int MinEmailLength = 7;
    internal const int MaxEmailLength = 254;
    internal const int MaxLocalPartLength = 64;

    [GeneratedRegex(
        @"^[^@\s]+@([a-z0-9]([a-z0-9-]*[a-z0-9])?\.)+[a-z]{2,}$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 250)]
    internal static partial Regex EmailRegex();
}