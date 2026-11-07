using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Models.Competitions;

public sealed record CreateCompetitionCategoryRequest
{
    [Display(Name = "Nama Kategori Lomba")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "{0} harus terdiri dari {2} sampai {1} karakter.")]
    public string Name { get; init; } = string.Empty;

    [Display(Name = "Status")]
    public bool IsActive { get; init; }
}

public sealed record UpdateCompetitionCategoryRequest
{
    [Display(Name = "Nama Kategori Lomba")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "{0} harus terdiri dari {2} sampai {1} karakter.")]
    public string Name { get; init; } = string.Empty;

    [Display(Name = "Status")]
    public bool IsActive { get; init; }
}