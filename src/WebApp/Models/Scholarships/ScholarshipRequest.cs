using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Models.Scholarships;

public sealed record CreateScholarshipRequest
{
    [Display(Name = "Nama Beasiswa")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "{0} harus terdiri dari {2} sampai {1} karakter.")]
    public string Name { get; init; } = string.Empty;

    [Display(Name = "Deskripsi")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "{0} harus terdiri dari {2} sampai {1} karakter.")]
    public string Description { get; init; } = string.Empty;

    [Display(Name = "Total Hadiah")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [Range(typeof(decimal), "1000", "100000000000", ErrorMessage = "{0} harus lebih dari Rp0.")]
    public decimal TotalPrize { get; init; }

    [Display(Name = "Gambar Poster atau Lainnya")]
    [Required(ErrorMessage = "{0} wajib diunggah.")]
    public IFormFile ContentImageFile { get; init; } = null!;

    [Display(Name = "Link Panduan")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [Url(ErrorMessage = "{0} harus berupa URL yang valid.")]
    [StringLength(2056, ErrorMessage = "{0} maksimal terdiri dari {1} karakter.")]
    public string GuideUrl { get; init; } = string.Empty;

    [Display(Name = "Link Registrasi")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [Url(ErrorMessage = "{0} harus berupa URL yang valid.")]
    [StringLength(2056, ErrorMessage = "{0} maksimal terdiri dari {1} karakter.")]
    public string RegistrationUrl { get; init; } = string.Empty;

    [Display(Name = "Persyaratan")]
    [Required(ErrorMessage = "Minimal satu {0} harus ditambahkan.")]
    [MinLength(1, ErrorMessage = "Minimal satu {0} harus ditambahkan.")]
    public List<string> Requirement { get; init; } = [];

    [Display(Name = "Contact Person")]
    [Required(ErrorMessage = "Minimal satu {0} harus ditambahkan.")]
    [MinLength(1, ErrorMessage = "Minimal satu {0} harus ditambahkan.")]
    public List<string> ContactPerson { get; init; } = [];

    [Display(Name = "Kategori Beasiswa")]
    [MinLength(1, ErrorMessage = "Pilih minimal satu kategori.")]
    public List<string> CategoryIds { get; set; } = [];

    [Display(Name = "Status Kelayakan")]
    public bool IsEligible { get; init; }

    [Display(Name = "Tanggal Buka")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    public DateTimeOffset RegistrationStartAt { get; init; }

    [Display(Name = "Tanggal Tutup")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    public DateTimeOffset RegistrationEndAt { get; init; }
}

public sealed record UpdateScholarshipRequest
{
    [Display(Name = "Nama Beasiswa")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "{0} harus terdiri dari {2} sampai {1} karakter.")]
    public string Name { get; init; } = string.Empty;

    [Display(Name = "Deskripsi")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "{0} harus terdiri dari {2} sampai {1} karakter.")]
    public string Description { get; init; } = string.Empty;

    [Display(Name = "Total Hadiah")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [Range(typeof(decimal), "1000", "100000000000", ErrorMessage = "{0} harus lebih dari Rp0.")]
    public decimal TotalPrize { get; init; }

    [Display(Name = "Gambar Poster, Browser, atau Lainnya")]
    public IFormFile? ContentImageFile { get; init; }

    [Display(Name = "Link Panduan")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [Url(ErrorMessage = "{0} harus berupa URL yang valid.")]
    [StringLength(2056, ErrorMessage = "{0} maksimal terdiri dari {1} karakter.")]
    public string GuideUrl { get; init; } = string.Empty;

    [Display(Name = "Link Registrasi")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [Url(ErrorMessage = "{0} harus berupa URL yang valid.")]
    [StringLength(2056, ErrorMessage = "{0} maksimal terdiri dari {1} karakter.")]
    public string RegistrationUrl { get; init; } = string.Empty;

    [Display(Name = "Persyaratan")]
    [Required(ErrorMessage = "Minimal satu {0} harus ditambahkan.")]
    [MinLength(1, ErrorMessage = "Minimal satu {0} harus ditambahkan.")]
    public List<string> Requirement { get; init; } = [];

    [Display(Name = "Contact Person")]
    [Required(ErrorMessage = "Minimal satu {0} harus ditambahkan.")]
    [MinLength(1, ErrorMessage = "Minimal satu {0} harus ditambahkan.")]
    public List<string> ContactPerson { get; init; } = [];

    [Display(Name = "Status Kelayakan")]
    public bool IsEligible { get; init; }

    [Display(Name = "Tanggal Buka")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    public DateTimeOffset RegistrationStartAt { get; init; }

    [Display(Name = "Tanggal Tutup")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    public DateTimeOffset RegistrationEndAt { get; init; }

    [Display(Name = "Kategori Beasiswa")]
    [MinLength(1, ErrorMessage = "Pilih minimal satu kategori.")]
    public List<string> CategoryIds { get; set; } = [];
}