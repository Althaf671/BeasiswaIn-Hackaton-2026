using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Mobile.Account;

public class ProfileModel : PageModel
{
    public string FullName { get; } = "Mustika Jr";
    public string Institution { get; } = "Universitas Andalas";
    public string Gpa { get; } = "3,72";
    public string Bio { get; } =
        "Mahasiswa Teknik Informatika tingkat akhir yang aktif dalam riset kecerdasan buatan dan pengembangan sistem edukasi terbuka. Berpengalaman memimpin proyek sosial teknologi di Sumatera Barat dan bertekad melanjutkan studi magister di bidang Computer Science dengan fokus beasiswa riset penuh.";
    public int Points { get; } = 590;
    public int PointGoal { get; } = 1000;

    public void OnGet()
    {
    }
}
