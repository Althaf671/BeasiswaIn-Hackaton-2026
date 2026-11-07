using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Models.Students;

public sealed record CreateStudentProfileRequest
{
    [Display(Name = "Institusi")]
    [StringLength(200, ErrorMessage = "{0} maksimal {1} karakter.")]
    public string? Institution { get; init; } = "Belum dimasukan";

    [Display(Name = "GPA")]
    [Range(typeof(decimal), "0", "9999999999999", ErrorMessage = "{0} harus berada di antara {1} dan {2}.")]
    public decimal? FinalScore { get; init; } = 0;

    [Display(Name = "Deskripsi")]
    [MaxLength(600, ErrorMessage = "{0} maksimal {1} karakter.")]
    public string? Description { get; init; }  = "Belum diisikan";
}

public sealed record UpdateStudentProfileRequest
{
    [Display(Name = "Nama Lengkap")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "{0} harus terdiri dari {2} sampai {1} karakter.")]
    public string FullName { get; init; } = string.Empty;

    [Display(Name = "Institusi")]
    [StringLength(200, ErrorMessage = "{0} maksimal {1} karakter.")]
    public string? Institution { get; init; } = "Belum dimasukan";

    [Display(Name = "GPA")]
    [Range(typeof(decimal), "0", "9999999999999", ErrorMessage = "{0} harus berada di antara {1} dan {2}.")]
    public decimal? FinalScore { get; init; } = 0;

    [Display(Name = "Deskripsi")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [StringLength(600, MinimumLength = 2, ErrorMessage = "{0} harus terdiri dari {2} sampai {1} karakter.")]
    public string Description { get; init; } = "Belum diisikan";
}