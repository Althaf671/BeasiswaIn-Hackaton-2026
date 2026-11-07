using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Mobile.Leaderboard;

public class IndexModel : PageModel
{
    public IReadOnlyList<RankRow> Rows { get; } =
    [
        new(4, "Rizky A.", "16 link disetujui", 890, true, "#1b5e20"),
        new(5, "Budi S.", "14 link disetujui", 750, false, "#587c3b"),
        new(6, "Nadia F.", "11 link disetujui", 620, false, "#4d7c5f"),
        new(7, "Fajar M.", "10 link disetujui", 540, false, "#5c832e"),
        new(8, "Siti W.", "9 link disetujui", 480, false, "#678a63"),
        new(9, "Reza P.", "Sulsel · 8 link disetujui", 410, false, "#587c3b"),
        new(10, "Maya T.", "Kaltim · 7 link disetujui", 370, false, "#4d7c5f"),
        new(11, "Aditya K.", "Jabar · 6 link disetujui", 330, false, "#5c832e"),
        new(12, "Putri N.", "Jateng · 6 link disetujui", 310, false, "#678a63"),
        new(13, "Hendra S.", "Sumut · 5 link disetujui", 280, false, "#1b5e20")
    ];

    public void OnGet()
    {
    }
}

public sealed record RankRow(int Rank, string Name, string Detail, int Points, bool IsYou, string Color);
