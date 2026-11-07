using BeasiswaIn.Web.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Mobile.Scholarships;

public class IndexModel : PageModel
{
    public IReadOnlyList<ExploreCard> Items { get; } =
    [
        new("Beasiswa LPDP Reguler", "Kemenkeu RI", "🎓", "98%", "Prioritas", true, "Fully Funded", "Batas: 25 Jul 2025", "#e6f4ed", "#cbebd9", false),
        new("BCA Finance Peduli", "PT BCA Finance", "💼", "89%", "S1", false, "Biaya Kuliah + Saku", "Batas: 10 Agt 2025", "#e0f2fe", "#cbe7f8", false),
        new("Beasiswa Unggulan", "Kemendikbudristek", "🏆", "96%", "S1 / S2", true, "Fully Funded", "Batas: 14 Agt 2025", "#e6f4ed", "#cbebd9", false),
        new("Djarum Beasiswa Plus", "Djarum Foundation", "🌱", "92%", "Prioritas", true, "Tunjangan Prestasi", "Batas: 30 Agt 2025", "#e6f4ed", "#cbebd9", false),
        new("Beasiswa Bank Indonesia", "Bank Indonesia", "🏦", "94%", "S1", true, "Fully Funded", "Batas: 05 Sep 2025", "#e0f2fe", "#cbe7f8", false),
        new("Sobat Bumi Pertamina", "Pertamina Foundation", "🌍", "88%", "Buka", true, "Fully Funded", "Batas: 20 Sep 2025", "#e6f4ed", "#cbebd9", false),
        new("Beasiswa Tanoto Foundation", "Tanoto Foundation", "📚", "90%", "S1", true, "Biaya Hidup", "Batas: 01 Okt 2025", "#e0f2fe", "#cbe7f8", false),
        new("CIMB Niaga Kejar Mimpi", "PT Bank CIMB Niaga", "🎯", "85%", "Buka", false, "Biaya Kuliah", "Batas: 15 Okt 2025", "#e6f4ed", "#cbebd9", false),
        new("Beasiswa Baznas RI", "BAZNAS", "🕌", "87%", "S1", true, "Bantuan UKT", "Batas: 28 Okt 2025", "#e0f2fe", "#cbe7f8", false),
        new("Beasiswa DataPrint", "DataPrint Indonesia", "💻", "86%", "Baru", true, "Pelatihan", "Batas: 10 Nov 2025", "#e6f4ed", "#cbebd9", false),
        new("Beasiswa Cargill Global", "Cargill & IIE", "🌐", "84%", "S2", false, "Fully Funded", "Batas: 25 Nov 2025", "#e0f2fe", "#cbe7f8", false),
        new("Beasiswa GrabScholar", "Grab Indonesia", "🛵", "83%", "S1", true, "Tunjangan", "Batas: 12 Des 2025", "#e6f4ed", "#cbebd9", false)
    ];

    public void OnGet()
    {
    }
}

