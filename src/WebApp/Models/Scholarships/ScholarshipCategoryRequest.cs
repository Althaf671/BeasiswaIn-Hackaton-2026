using System.ComponentModel.DataAnnotations;

namespace BeasiswaIn.Web.Models.Scholarships;

public sealed record CreateScholarshipCategoryRequest
{
    [Display(Name = "Nama Beasiswa")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "{0} harus terdiri dari {2} sampai {1} karakter.")]
    public string Name { get; init; } = string.Empty;

    [Display(Name = "Status")]
    public bool IsActive { get; init; } = true;
}

public sealed record UpdateScholarshipCategoryRequest
{
    [Display(Name = "Nama Kategori Beasiswa")]
    [Required(ErrorMessage = "{0} wajib diisi.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "{0} harus terdiri dari {2} sampai {1} karakter.")]
    public string Name { get; init; } = string.Empty;

    [Display(Name = "Status")]
    public bool IsActive { get; init; }
}