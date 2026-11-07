using System.Text.Json;

namespace BeasiswaIn.Web.Common.Helpers;

public sealed record RecommendedScholarship(
    string ScholarshipId,
    string Name,
    int MatchScore,
    string MatchReason,
    string Feedback);

internal static class ChatBotParserHelper
{
    public static List<RecommendedScholarship> Parse(string? chatResult)
    {
        var result = new List<RecommendedScholarship>();
        if (string.IsNullOrWhiteSpace(chatResult))
            return result;

        // Buang pembungkus ```json ... ``` atau teks liar di luar JSON
        var start = chatResult.IndexOf('{');
        var end = chatResult.LastIndexOf('}');
        if (start < 0 || end <= start)
            return result;

        try
        {
            using var doc = JsonDocument.Parse(chatResult[start..(end + 1)]);

            if (!doc.RootElement.TryGetProperty("recommendations", out var items)
                || items.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var item in items.EnumerateArray())
            {
                var id = ReadString(item, "opportunityId");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (result.Any(r => r.ScholarshipId.Equals(id, StringComparison.OrdinalIgnoreCase)))
                    continue;

                result.Add(new RecommendedScholarship(
                    id,
                    ReadString(item, "opportunityName"),
                    ReadInt(item, "matchScore"),
                    ReadString(item, "matchReason"),
                    ReadString(item, "feedback")));
            }
        }
        catch (JsonException)
        {
            // Output AI bukan JSON valid: anggap tidak ada rekomendasi
        }

        return result;
    }

    public static List<string> ExtractScholarshipIds(string? chatResult) =>
        Parse(chatResult).Select(r => r.ScholarshipId).ToList();

    private static string ReadString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return string.Empty;

        return p.ValueKind switch
        {
            JsonValueKind.String => p.GetString()?.Trim() ?? string.Empty,
            JsonValueKind.Number => p.GetRawText(),
            _ => string.Empty
        };
    }

    private static int ReadInt(JsonElement el, string name)
    {
        if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number)
            return p.TryGetInt32(out var i) ? i : (int)Math.Round(p.GetDouble());

        return 0;
    }
}