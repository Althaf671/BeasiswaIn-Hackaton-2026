using System.Text.Json;
using BeasiswaIn.Web.Models.ScholarBot;

namespace BeasiswaIn.Web.Services.ScholarBot;

internal sealed partial class ScholarBotTools
{
    private const int MaxCandidates = 10;

    private async Task<ScholarBotToolResult> GetRecommendationsAsync(JsonElement args, CancellationToken cancellationToken)
    {
        if (!HasOnly(args, "essay", "categoryIds", "limit") ||
            !TryString(args, "essay", 1, 2000, out var essay))
            return Failure("INVALID_ARGUMENTS", "Sertakan konteks pengguna dalam essay (1–2.000 karakter); jangan mengarang profil.");
        var limit = 5;
        if (args.TryGetProperty("limit", out var rawLimit) &&
            (rawLimit.ValueKind != JsonValueKind.Number || !rawLimit.TryGetInt32(out limit) || limit is < 1 or > 5))
            return Failure("INVALID_ARGUMENTS", "Limit rekomendasi harus antara 1 dan 5.");
        List<string> categoryIds;
        if (args.TryGetProperty("categoryIds", out var rawIds))
        {
            if (rawIds.ValueKind != JsonValueKind.Array || rawIds.GetArrayLength() > 15)
                return Failure("INVALID_ARGUMENTS", "Pilih maksimal 15 kategori.");
            categoryIds = [];
            foreach (var id in rawIds.EnumerateArray())
            {
                if (id.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(id.GetString()) || id.GetString()!.Length > 64)
                    return Failure("INVALID_ARGUMENTS", "ID kategori tidak valid.");
                var value = id.GetString()!.Trim();
                if (!categoryIds.Contains(value)) categoryIds.Add(value);
            }
        }
        else categoryIds = await GetSavedCategoryIdsAsync(cancellationToken);
        if (categoryIds.Count == 0)
            return Failure("PREFERENCES_REQUIRED", "Belum ada kategori untuk pencarian. Tanyakan minat pengguna lalu gunakan kategori valid dari GetUserProfile; jangan memilih secara acak.");
        var optionsResult = await categories.GetOptionsAsync(cancellationToken);
        if (optionsResult.IsFailure)
            return Failure("SERVICE_UNAVAILABLE", "Kategori beasiswa belum dapat dibaca.");
        var selected = optionsResult.Value!.Where(c => categoryIds.Contains(c.Id)).ToArray();
        if (selected.Length != categoryIds.Count)
            return Failure("INVALID_CATEGORY", "Ada kategori yang tidak aktif atau tidak ditemukan. Baca ulang kategori yang tersedia.");
        var candidatesResult = await scholarships.GetEligibleByCategoryIdsAsync(categoryIds, cancellationToken);
        if (candidatesResult.IsFailure)
            return Failure("SERVICE_UNAVAILABLE", "Kandidat beasiswa belum dapat dibaca.");
        var now = timeProvider.GetUtcNow();
        var open = candidatesResult.Value!
            .Where(s => s.RegistrationStartAt <= now && s.RegistrationEndAt >= now)
            .OrderBy(s => s.RegistrationEndAt).ThenBy(s => s.Id).ToArray();
        if (open.Length == 0)
            return Success(new { status = "no_matches", recommendations = Array.Empty<object>(),
                selectionBasis = "kategori dan periode pendaftaran", checkedAt = now,
                message = "Tidak ada beasiswa yang sedang buka untuk kategori tersebut. Ini bukan kesimpulan bahwa pengguna tidak memenuhi syarat." });
        var considered = open.Take(MaxCandidates).Select(s => s with
        {
            Description = Limit(s.Description, 600),
            Requirement = s.Requirement.Take(8).Select(r => Limit(r, 350)).ToList(),
            ContactPerson = [], ContentImageUrl = string.Empty
        }).ToArray();
        var analysis = await matching.AnalyzeBestScholarshipChoices(essay!,
            "{\"certificateProvided\":false,\"message\":\"Tidak ada sertifikat yang diberikan dalam konsultasi ini. Jangan mengasumsikan prestasi atau nilai sertifikat.\"}",
            selected.Select(c => c.Name).ToArray(), considered, cancellationToken);
        using var output = JsonDocument.Parse(UnwrapJson(analysis));
        if (output.RootElement.ValueKind != JsonValueKind.Object ||
            !output.RootElement.TryGetProperty("recommendations", out var recommendations) ||
            recommendations.ValueKind != JsonValueKind.Array || recommendations.GetArrayLength() > 5)
            return Failure("INVALID_MATCHING_OUTPUT", "Keluaran matching belum dapat divalidasi terhadap kandidat database.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<object>(limit);
        var sources = new List<ScholarBotSource>(limit);
        foreach (var recommendation in recommendations.EnumerateArray())
        {
            if (recommendation.ValueKind != JsonValueKind.Object ||
                !TryString(recommendation, "opportunityId", 1, 64, out var id) || !seen.Add(id!) ||
                !TryString(recommendation, "matchReason", 1, 1600, out var reason) ||
                !TryString(recommendation, "feedback", 1, 1600, out var feedback) ||
                !TryString(recommendation, "matchScoreSource", 1, 1600, out var scoreSource) ||
                !recommendation.TryGetProperty("matchScore", out var rawScore) ||
                rawScore.ValueKind != JsonValueKind.Number || !rawScore.TryGetDecimal(out var score) || score is < 0 or > 100)
                return Failure("INVALID_MATCHING_OUTPUT", "Matching menghasilkan skor, ID, atau penjelasan yang tidak valid.");
            var candidate = considered.FirstOrDefault(s => s.Id == id);
            if (candidate is null)
                return Failure("INVALID_MATCHING_OUTPUT", "Matching menghasilkan ID di luar kandidat database.");
            if (result.Count >= limit) continue;
            result.Add(new { opportunityId = candidate.Id, opportunityName = candidate.Name,
                matchReason = reason, feedback, matchScore = score, matchScoreSource = scoreSource,
                registrationStartAt = candidate.RegistrationStartAt, registrationEndAt = candidate.RegistrationEndAt,
                registrationStatus = "open" });
            sources.Add(Source(candidate));
        }
        return Success(new
        {
            status = result.Count == 0 ? "no_matches" : "ok", recommendations = result,
            selectionBasis = "kategori + analisis AI existing, bukan pemeriksaan kelayakan deterministik",
            checkedAt = now, openCandidateCount = open.Length, analyzedCandidateCount = considered.Length,
            candidateSelection = "maksimal 10 kandidat dengan deadline terdekat",
            coverageLimited = open.Length > MaxCandidates,
            limitations = new[]
            {
                "Skor merupakan estimasi AI, bukan persentase peluang diterima atau jaminan memenuhi syarat.",
                "Tidak ada sertifikat tersimpan yang dibaca; deskripsi dan persyaratan kandidat dibatasi untuk konteks AI. Baca GetScholarshipDetails sebelum menyusun checklist."
            }
        }, sources);
    }
}
