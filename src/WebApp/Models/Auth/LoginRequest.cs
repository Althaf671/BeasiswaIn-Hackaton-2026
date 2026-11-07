
using System.ComponentModel.DataAnnotations;
using BeasiswaIn.Web.Common.Constants;

namespace BeasiswaIn.Web.Models.Auth;

public sealed record LoginRequest
{
    [Display(Name = "Alamat Email")]
    [Required(ErrorMessage = "Alamat email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format alamat email tidak valid.")]
    [MaxLength(EmailConstant.MaxEmailLength, ErrorMessage = "Alamat email terlalu panjang.")]
    public string Email { get; init; } = string.Empty;

    [Display(Name = "Kata Sandi")]
    [Required(ErrorMessage = "Kata sandi wajib diisi.")]
    [MaxLength(IdentityConstant.MaxPasswordLength, ErrorMessage = "Kata sandi terlalu panjang.")]
    public string Password { get; init; } = string.Empty;

    [Display(Name = "Ingat Saya")]
    public bool RememberMe { get; init; }
}

public sealed record LoginOAuthRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}