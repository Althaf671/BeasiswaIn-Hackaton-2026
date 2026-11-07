using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Mobile.Chat;

public class IndexModel : PageModel
{
    public IReadOnlyList<ChatHistoryGroup> History { get; } =
    [
        new("Hari Ini",
        [
            "Rekomendasi beasiswa S1 IPK 3.72",
            "Syarat beasiswa LPDP Reguler 2026"
        ]),
        new("Kemarin",
        [
            "Beasiswa BAZNAS & riset skripsi",
            "Tips esai motivasi Beasiswa Djarum"
        ]),
        new("7 Hari Terakhir",
        [
            "Beasiswa kuliah luar negeri fully funded"
        ])
    ];

    public int HistoryCount => History.Sum(group => group.Titles.Count);

    public void OnGet()
    {
    }
}

public record ChatHistoryGroup(string Label, IReadOnlyList<string> Titles);
