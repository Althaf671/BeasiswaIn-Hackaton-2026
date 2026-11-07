using BeasiswaIn.Web.Infrastructures.DependencyInjection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BeasiswaIn.Web.Pages.Students.Notifications;

[EnableRateLimiting(RateLimitRegistry.LowRateResourcePolicy)]
public class IndexModel : PageModel
{
    public IReadOnlyList<NotificationItem> Items { get; } =
    [
        new(NotificationStatus.Approved, "Link beasiswamu disetujui!",
            "Beasiswa Unggulan Nusantara sudah diperiksa admin dan kini tampil sebagai terverifikasi di katalog.",
            "5 menit lalu", false, "Beasiswa Unggulan Nusantara"),
        new(NotificationStatus.Rejected, "Link beasiswamu belum dapat disetujui",
            "Beasiswa Pendidikan Daerah Sumbar. Alasan: sumber tidak dapat ditelusuri ke situs resmi penyelenggara.",
            "2 jam lalu", false, null),
        new(NotificationStatus.Approved, "Link beasiswamu disetujui!",
            "Prestasi Mahasiswa 2026 kini tampil di katalog.",
            "Kemarin", true, "Prestasi Mahasiswa 2026"),
        new(NotificationStatus.Review, "Link kamu sedang diperiksa admin",
            "Beasiswa Riset Nasional sudah kami terima. Kami kabari setelah selesai diperiksa.",
            "2 hari lalu", true, null),
        new(NotificationStatus.Approved, "Link beasiswamu disetujui!",
            "Beasiswa Berbakat Digital kini tampil di katalog.",
            "4 hari lalu", true, "Beasiswa Berbakat Digital")
    ];

    public int Count(NotificationStatus? status) =>
        status is null ? Items.Count : Items.Count(item => item.Status == status);

    public void OnGet()
    {
    }
}

public enum NotificationStatus
{
    Approved,
    Rejected,
    Review
}

public record NotificationItem(
    NotificationStatus Status,
    string Title,
    string Body,
    string Time,
    bool IsRead,
    string? ScholarshipName);
