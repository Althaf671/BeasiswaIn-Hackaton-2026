using System.Text.Json;
using BeasiswaIn.Web.Models.ScholarBot;
using Microsoft.EntityFrameworkCore;

namespace BeasiswaIn.Web.Services.ScholarBot;

internal sealed partial class ScholarBotTools
{
    private async Task<ScholarBotToolResult> GetDetailsAsync(JsonElement args, CancellationToken cancellationToken)
    {
        if (!HasOnly(args, "scholarshipId", "name"))
            return Failure("INVALID_ARGUMENTS", "Gunakan scholarshipId atau name, bukan parameter lain.");
        var hasId = args.TryGetProperty("scholarshipId", out _);
        var hasName = args.TryGetProperty("name", out _);
        if (hasId == hasName)
            return Failure("INVALID_ARGUMENTS", "Sertakan tepat satu scholarshipId atau name.");
        string? id;
        if (hasId)
        {
            if (!TryString(args, "scholarshipId", 1, 64, out id))
                return Failure("INVALID_ARGUMENTS", "ID beasiswa tidak valid.");
        }
        else
        {
            if (!TryString(args, "name", 3, 160, out var name))
                return Failure("INVALID_ARGUMENTS", "Nama pencarian harus terdiri dari 3–160 karakter.");
            var term = name!.ToLowerInvariant();
            var matches = await database.Scholarships.AsNoTracking()
                .Where(s => s.IsEligible && s.Name.ToLower().Contains(term))
                .OrderBy(s => s.Name).ThenBy(s => s.Id)
                .Select(s => new { s.Id, s.Name }).Take(6).ToListAsync(cancellationToken);
            if (matches.Count == 0) return Failure("NOT_FOUND", "Beasiswa tidak ditemukan dalam katalog yang tersedia.");
            if (matches.Count > 1)
                return Success(new { status = "choose_scholarship", matches = matches.Take(5), hasMore = matches.Count > 5 });
            id = matches[0].Id;
        }
        var detail = await scholarships.GetByIdAsync(id!, cancellationToken);
        if (detail.IsFailure || !detail.Value!.IsEligible)
            return Failure("NOT_FOUND", "Beasiswa tidak ditemukan dalam katalog yang tersedia.");
        var scholarship = detail.Value;
        var now = timeProvider.GetUtcNow();
        var description = Limit(scholarship.Description, 2000);
        var requirements = scholarship.Requirement.Take(20).Select(r => Limit(r, 500)).ToArray();
        return Success(new
        {
            scholarship.Id, scholarship.Name, description, scholarship.TotalPrize,
            requirements, contactPerson = scholarship.ContactPerson.Take(5).Select(c => Limit(c, 150)),
            categories = scholarship.CategoryIds,
            scholarship.RegistrationStartAt, scholarship.RegistrationEndAt,
            registrationStatus = scholarship.RegistrationEndAt < now ? "closed" : scholarship.RegistrationStartAt > now ? "upcoming" : "open",
            registrationUrl = SafeUrl(scholarship.RegistrationUrl), guideUrl = SafeUrl(scholarship.GuideUrl),
            checkedAt = now,
            contentTruncated = scholarship.Description.Length > 2000 || scholarship.Requirement.Count > 20 || scholarship.Requirement.Any(r => r.Length > 500),
            limitation = "Persyaratan merupakan informasi sumber, bukan hasil pemeriksaan profil pengguna. Gunakan tautan panduan untuk dokumen lengkap."
        }, [Source(scholarship)]);
    }
}
