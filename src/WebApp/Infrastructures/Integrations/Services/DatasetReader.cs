using System.Globalization;
using BeasiswaIn.Web.Infrastructures.Persistance;
using BeasiswaIn.Web.Interfaces;

namespace BeasiswaIn.Web.Infrastructures.Integrations.Services;

internal sealed class DatasetReader : IDatasetReader
{
    private static readonly string[] RequiredColumns =
    [
        "Name",
        "Description",
        "TotalPrize",
        "ContentImageUrl",
        "GuideUrl",
        "RegistrationUrl",
        "Requirement",
        "ContactPerson",
        "IsEligible",
        "RegistrationStartAt",
        "RegistrationEndAt"
    ];

    public async Task<IReadOnlyList<ScholarshipDatasetRow>> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Dataset tidak ditemukan.", filePath);

        using var reader = new StreamReader(filePath);

        var headerLine = await reader.ReadLineAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidDataException("Dataset CSV kosong.");

        var headers = ParseCsvLine(headerLine);

        var missingColumns = RequiredColumns
            .Where(column => !headers.Contains(column, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (missingColumns.Length > 0)
            throw new InvalidDataException(
                $"Kolom CSV tidak ditemukan: {string.Join(", ", missingColumns)}");

        var columnIndexes = RequiredColumns.ToDictionary(
            column => column,
            column => headers.FindIndex(
                header => string.Equals(
                    header,
                    column,
                    StringComparison.OrdinalIgnoreCase)));

        var result = new List<ScholarshipDatasetRow>();

        var lineNumber = 1;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var recordStartLine = lineNumber;

            try
            {
                // A quoted CSV field may span multiple physical lines. Escaped quotes
                // occur in pairs, so only an odd quote count leaves a field open.
                var record = new System.Text.StringBuilder(line);
                var insideQuotes = line.Count(character => character == '"') % 2 != 0;

                while (insideQuotes)
                {
                    var continuation = await reader.ReadLineAsync(cancellationToken);
                    if (continuation is null)
                        throw new FormatException("CSV memiliki quoted field yang tidak ditutup.");

                    lineNumber++;
                    record.Append('\n').Append(continuation);
                    if (continuation.Count(character => character == '"') % 2 != 0)
                        insideQuotes = !insideQuotes;
                }

                var fields = ParseCsvLine(record.ToString());

                string Get(string column)
                {
                    var index = columnIndexes[column];

                    return index < fields.Count ? NormalizeEmpty(fields[index]) : string.Empty;
                }

                result.Add(new ScholarshipDatasetRow
                {
                    Name = Get("Name"),
                    Description = Get("Description"),
                    TotalPrize = ParseDecimal(Get("TotalPrize"), "TotalPrize"),
                    ContentImageUrl = Get("ContentImageUrl"),
                    GuideUrl = Get("GuideUrl"),
                    RegistrationUrl = Get("RegistrationUrl"),
                    Requirement = ParseStringList(Get("Requirement")),
                    ContactPerson = ParseStringList(Get("ContactPerson")),
                    IsEligible = ParseBoolean(Get("IsEligible"), "IsEligible"),
                    RegistrationStartAt = ParseDateTimeOffset(
                        Get("RegistrationStartAt"), "RegistrationStartAt"),
                    RegistrationEndAt = ParseDateTimeOffset(
                        Get("RegistrationEndAt"), "RegistrationEndAt")
                });
            }
            catch (Exception ex) when (
                ex is FormatException or InvalidDataException or OverflowException or System.Text.Json.JsonException)
            {
                throw new InvalidDataException(
                    $"Dataset tidak valid pada baris {recordStartLine}: {ex.Message}", ex);
            }
        }

        return result;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var insideQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];

            if (character == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (character == ',' && !insideQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        if (insideQuotes)
            throw new FormatException("CSV memiliki quoted field yang tidak ditutup.");

        fields.Add(current.ToString());

        return fields;
    }

    // The destination properties are non-nullable; missing CSV values use their
    // empty/default representation rather than rejecting the entire import.
    private static string NormalizeEmpty(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0 || trimmed is "''" or "\"\"" ||
            string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : value;
    }

    private static decimal ParseDecimal(string value, string column)
    {
        if (value.Length == 0)
            return 0m;

        if (!decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var result))
            throw new FormatException($"Kolom '{column}' bukan decimal yang valid.");

        return result;
    }

    private static bool ParseBoolean(string value, string column)
    {
        if (value.Length == 0)
            return false;

        value = value.Trim();
        if (bool.TryParse(value, out var result))
            return result;

        return value switch
        {
            "1" => true,
            "0" => false,
            _ => throw new FormatException($"Kolom '{column}' bukan boolean yang valid.")
        };
    }

    private static DateTimeOffset ParseDateTimeOffset(string value, string column)
    {
        if (value.Length == 0)
            return DateTimeOffset.MinValue;

        if (!DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var result))
            throw new FormatException($"Kolom '{column}' bukan tanggal yang valid.");

        return result.ToUniversalTime();
    }

    private static List<string> ParseStringList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        value = value.Trim();
        if (value.StartsWith('['))
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(value) ?? [];

        return value.Split(
            ';',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        ).ToList();
    }
}
