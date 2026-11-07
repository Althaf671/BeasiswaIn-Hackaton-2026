
 
using System.Globalization;

namespace BeasiswaIn.Web.Common.Helpers;

public static class NumberFormatter
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    // 1500000 -> "Rp 1.500.000"
    public static string Rupiah(decimal value) =>
        $"Rp {value.ToString("N0", Id)}";

    public static string Rupiah(decimal? value) =>
        value.HasValue ? Rupiah(value.Value) : "-";

    // 90 -> "90 menit"
    public static string Minutes(int value) =>
        $"{value.ToString("N0", Id)} menit";

    public static string Minutes(int? value) =>
        value.HasValue ? Minutes(value.Value) : "-";

    // 1500000 -> "1.500.000"
    public static string Number(decimal value) =>
        value.ToString("N0", Id);

    // "Rp 1.500.000" / "90 menit" 
    public static bool TryParseDigits(string? input, out long result)
    {
        var digits = new string((input ?? string.Empty).Where(char.IsDigit).ToArray());
        return long.TryParse(digits, out result);
    }

    // 0822... / 822... / 62822... / +62822... -> 62822...
    public static string ToWhatsApp(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return string.Empty;

        if (digits.StartsWith("62")) digits = digits[2..];
        if (digits.StartsWith("0")) digits = digits[1..];

        return digits.Length == 0 ? string.Empty : "62" + digits;
    }

    public static string PhoneNumberFormatter(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return string.Empty;

        if (digits.StartsWith("62")) digits = digits[2..];
        if (digits.StartsWith("0")) digits = digits[1..];

        if (digits.Length < 9 || digits.Length > 12 || digits[0] != '8')
            return input.Trim();

        var parts = new List<string> { digits[..3] };
        for (var i = 3; i < digits.Length; i += 4)
            parts.Add(digits[i..Math.Min(i + 4, digits.Length)]);

        return "+62 " + string.Join("-", parts);
    }
}