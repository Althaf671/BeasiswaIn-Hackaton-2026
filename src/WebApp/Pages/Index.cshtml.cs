using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages;

public class IndexModel : PageModel
{
    public string UserName { get; } = "Dimas";

    public IReadOnlyList<ExploreCard> Popular { get; } =
    [
        new("Beasiswa LPDP Reguler", "Kemenkeu RI", "🎓", "98%", "Prioritas", true, "Fully Funded", "Batas: 15 Juli 2025", "#e6f4ed", "#cbebd9", true),
        new("Beasiswa BCA Finance", "PT Bank Central Asia", "💼", "89%", "S1 / D4", false, "Biaya Kuliah + Saku", "Batas: 30 Juni 2025", "#e0f2fe", "#cbe7f8", false),
        new("Beasiswa Bank Indonesia", "Bank Indonesia", "🏦", "94%", "Prioritas", true, "Fully Funded", "Batas: 05 Sep 2025", "#e6f4ed", "#cbebd9", false),
        new("Beasiswa Tanoto Foundation", "Tanoto Foundation", "📚", "90%", "S1", true, "Biaya Hidup", "Batas: 01 Okt 2025", "#e0f2fe", "#cbe7f8", false),
        new("Beasiswa Baznas RI", "BAZNAS", "🕌", "87%", "Buka", true, "Bantuan UKT", "Batas: 28 Okt 2025", "#e6f4ed", "#cbebd9", false)
    ];

    public IReadOnlyList<ExploreCard> Favorite { get; } =
    [
        new("Beasiswa Unggulan", "Kemendikbudristek", "🏛️", "96%", "S1 / S2", true, "Fully Funded", "Batas: 10 Agt 2025", "#e6f4ed", "#cbebd9", true),
        new("Djarum Beasiswa Plus", "Djarum Foundation", "⚡", "92%", "Prioritas", true, "Tunjangan Prestasi", "Batas: 25 Mei 2025", "#e6f4ed", "#cbebd9", false),
        new("Sobat Bumi Pertamina", "Pertamina Foundation", "🌍", "88%", "S1", true, "Fully Funded", "Batas: 20 Sep 2025", "#e0f2fe", "#cbe7f8", false),
        new("CIMB Niaga Kejar Mimpi", "PT Bank CIMB Niaga", "🎯", "85%", "Buka", false, "Biaya Kuliah", "Batas: 15 Okt 2025", "#e6f4ed", "#cbebd9", false),
        new("Beasiswa GrabScholar", "Grab Indonesia", "🛵", "83%", "S1", true, "Tunjangan", "Batas: 12 Des 2025", "#e0f2fe", "#cbe7f8", false)
    ];

    public IReadOnlyList<ExploreCard> Latest { get; } =
    [
        new("Bangkit Academy Google", "Google, GoTo, Traveloka", "💻", "95%", "5 hari lagi", true, "Sertifikasi Global", "Batas: 5 Hari Lagi", "#e6f4ed", "#cbebd9", true),
        new("IDCamp Indosat", "Indosat Ooredoo", "🚀", "91%", "Buka", true, "Bootcamp", "Batas: 18 Agu 2025", "#e6f4ed", "#cbebd9", false),
        new("Beasiswa DataPrint", "DataPrint Indonesia", "🖨️", "86%", "Baru", true, "Pelatihan", "Batas: 10 Nov 2025", "#e0f2fe", "#cbe7f8", false),
        new("Beasiswa Cargill Global", "Cargill & IIE", "🌐", "84%", "S2", false, "Fully Funded", "Batas: 25 Nov 2025", "#e6f4ed", "#cbebd9", false),
        new("Beasiswa Paragon", "ParagonCorp", "✨", "82%", "Baru", true, "Ikatan Dinas", "Batas: 02 Des 2025", "#e0f2fe", "#cbe7f8", false)
    ];

    public void OnGet()
    {
    }
}

public sealed record ExploreCard(
    string Name,
    string Provider,
    string Emoji,
    string Match,
    string Badge,
    bool BadgeGreen,
    string Funding,
    string Deadline,
    string From,
    string To,
    bool Sparkle);
