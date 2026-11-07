using Microsoft.AspNetCore.Authentication;
using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Models.Auth;

namespace BeasiswaIn.Web.Interfaces;

public interface IIdentityService
{
    public Task<Result<string>> RegisterUserAccountAsync(string username, string password, string email, string role, bool emailConfirmed);
    public Task<Result<string>> LoginAsync(LoginRequest request);
    public Result<AuthenticationProperties> CreateGoogleLoginProperties(string callbackUrl);
    public Task<Result> LoginWithGoogleAsync(string? remoteError = null);
    public Task<Result> LogoutAsync();
    public Task<Result<GetMyAccountResponse>> GetMyAccountAsync();
    public Task<Result<TwoFactorStatusResponse>> GetTwoFactorStatusAsync();
    public Task<Result<AuthenticatorSetupResponse>> GetAuthenticatorSetupAsync();
    public Task<Result<string[]>> EnableAuthenticatorAsync(EnableAuthenticatorRequest request);
    public Task<Result> DisableTwoFactorAsync();
    public Task<Result> ResetAuthenticatorAsync();
    public Task<Result<string[]>> GenerateRecoveryCodesAsync();

    public Task<Result<string>> LoginWith2faAsync(LoginWith2faRequest request);
    public Task<Result<string>> LoginWithRecoveryCodeAsync(LoginWithRecoveryCodeRequest request);

    public Task<Result> ChangePasswordAsync(ChangePasswordRequest request);
    public Task<Result> SetPhoneNumberAsync(SetPhoneNumberRequest request);
    public Task<Result<string>> RequestEmailChangeAsync(string newEmail);  
    public Task<Result> ConfirmEmailChangeAsync(ConfirmEmailChangeRequest request);
}