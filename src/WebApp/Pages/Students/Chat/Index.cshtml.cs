using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BeasiswaIn.Web.Pages.Students.Chat;

[Authorize]
[EnableRateLimiting(RateLimitRegistry.LowRateResourcePolicy)]
public class IndexModel : PageModel
{
    public const int MaxMessageLength = 12000;
    public const int MaxHistoryMessages = 12;
    public const int MaxContextLength = 48000;
    public const int MaxAnswerLength = 24000;

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
        Response.Headers.CacheControl = "no-store";
    }
}

public record ChatHistoryGroup(string Label, IReadOnlyList<string> Titles);