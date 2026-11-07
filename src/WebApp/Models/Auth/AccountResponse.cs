 
namespace BeasiswaIn.Web.Models.Auth;

public sealed record GetMyAccountResponse
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IList<string> Role { get; init; } = [];
    public bool IsEmailConfirmed { get; init; }
    public string Phone { get; init; } = string.Empty;
    public bool IsPhoneConfirmed { get; init; }
    public bool IsTwoFactorEnabled { get; init; } 
}