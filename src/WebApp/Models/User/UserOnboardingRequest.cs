using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Models.User;

public sealed record UserScholarshipOnBoardingRequest
{
    [Display(Name = "Essay Bebas")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [StringLength(500, MinimumLength = 20, ErrorMessage = "{0} harus terdiri dari {2} sampai {1} karakter.")]
    public string Essay { get; init; } = string.Empty;

    [Display(Name = "Kategori Beasiswa")]
    [MinLength(1, ErrorMessage = "Pilih minimal satu kategori.")]
    [MaxLength(20, ErrorMessage = "Terlalu banyak kategori dipilih.")]
    public List<string> CategoryIds { get; init; } = [];

    [Display(Name = "Sertifikat")]
    [Required(ErrorMessage = "Harus unggah satu sertifikat.")]
    public IFormFile Certificates { get; init; } = null!;
}