using Microsoft.AspNetCore.Html;

namespace PhotographyStudio.Web.Pages.Dashboard.Shared;

public class FormDrawerViewModel
{
    /// <summary>Id elemen drawer; dipakai sebagai data-bs-target di tombol pemicu (#Id).</summary>
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>Handler Razor Pages, mis. "Create" -> OnPostCreate. Kosong = OnPost.</summary>
    public string? Handler { get; init; }

    /// <summary>Opsional: post ke halaman lain. Kosong = halaman saat ini.</summary>
    public string? Page { get; init; }

    public string SubmitText { get; init; } = "Simpan";
    public string CancelText { get; init; } = "Batal";

    /// <summary>Lebar drawer, mis. "480px". Kosong = bawaan Bootstrap (400px).</summary>
    public string? Width { get; init; }

    /// <summary>true = drawer terbuka otomatis saat halaman dimuat (mis. setelah validasi server gagal).</summary>
    public bool OpenOnLoad { get; init; }

    /// <summary>true = klik di luar drawer tidak menutupnya (cegah isian hilang tak sengaja).</summary>
    public bool StaticBackdrop { get; init; }

    /// <summary>true = enctype multipart/form-data (untuk upload file).</summary>
    public bool Multipart { get; init; }

    /// <summary>Isi form; diisi dengan Razor template (@&lt;div&gt;...&lt;/div&gt;).</summary>
    public Func<object?, IHtmlContent>? Body { get; init; }
}