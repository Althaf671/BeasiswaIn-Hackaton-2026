using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeasiswaIn.Web.Pages.Mobile.Scholarships;

public class DetailModel : PageModel
{
    public string Name { get; private set; } = "Beasiswa Unggulan Nusantara 2026";

    public string Provider { get; private set; } = "Yayasan Bakti Pendidikan Nusantara & Kemendikbudristek";

    public string Funding { get; private set; } = "Fully Funded";

    public string Level { get; private set; } = "S1 / Sarjana";

    public string Deadline { get; private set; } = "30 Nov 2026";

    public string? DaysLeft { get; private set; } = "24 Hari";

    public void OnGet(string? nama, string? penyedia, string? dana, string? jenjang, string? batas)
    {
        if (!string.IsNullOrWhiteSpace(nama)) Name = nama;
        if (!string.IsNullOrWhiteSpace(penyedia)) Provider = penyedia;
        if (!string.IsNullOrWhiteSpace(dana)) Funding = dana;
        if (!string.IsNullOrWhiteSpace(jenjang)) Level = jenjang;
        if (!string.IsNullOrWhiteSpace(batas))
        {
            Deadline = batas.Replace("Batas:", "", StringComparison.OrdinalIgnoreCase).Trim();
            DaysLeft = null;
        }
    }
}
