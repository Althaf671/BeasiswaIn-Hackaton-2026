using System.Globalization;
using BeasiswaIn.Web.Models.Scholarships;
using BeasiswaIn.Web.Pages.Shared;

namespace BeasiswaIn.Web.Common.Helpers;

public static class ExploreCardMapper
{
    private static readonly CultureInfo IdCulture = CultureInfo.GetCultureInfo("id-ID");

    public static ExploreCard From(ScholarshipResponse s, int index, DateTimeOffset now)
    {
        var even = index % 2 == 0;

        return new ExploreCard
        {
            Id = s.Id,
            Name = s.Name,
            Provider = Excerpt(s.Description, 40),
            Emoji = "🎓",
            Match = s.IsEligible ? "Layak" : "Cek syarat",
            Badge = StatusLabel(s, now),
            BadgeGreen = now >= s.RegistrationStartAt && now <= s.RegistrationEndAt,
            Funding = s.TotalPrize > 0 ? $"Rp {s.TotalPrize.ToString("N0", IdCulture)}" : "Lihat detail",
            Deadline = $"Batas: {s.RegistrationEndAt.ToLocalTime().ToString("dd MMM yyyy", IdCulture)}",
            From = even ? "#e6f4ed" : "#e0f2fe",
            To = even ? "#cbebd9" : "#cbe7f8",
            Sparkle = index == 0,
            ImageUrl = s.ContentImageUrl,
        };
    }

    private static string StatusLabel(ScholarshipResponse s, DateTimeOffset now)
    {
        if (now < s.RegistrationStartAt)
            return "Segera";

        if (now > s.RegistrationEndAt)
            return "Ditutup";

        var daysLeft = (int)Math.Ceiling((s.RegistrationEndAt - now).TotalDays);
        return daysLeft <= 7 ? $"{daysLeft} hari lagi" : "Buka";
    }

    private static string Excerpt(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Beasiswa";

        var clean = text.Trim().ReplaceLineEndings(" ");
        return clean.Length <= max ? clean : clean[..max].TrimEnd() + "…";
    }
}