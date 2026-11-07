 
namespace BeasiswaIn.Web.Models.Auth;

public sealed record AuthenticatorSetupResponse(string SharedKey, string AuthenticatorUri);
public sealed record TwoFactorStatusResponse(bool HasAuthenticator, bool Is2faEnabled, int RecoveryCodesLeft);

public sealed record EnableAuthenticatorRequest(string Code);
public sealed record LoginWith2faRequest(string Code, bool RememberMe, bool RememberMachine);
public sealed record LoginWithRecoveryCodeRequest(string RecoveryCode);

public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);
public sealed record SetPhoneNumberRequest(string? PhoneNumber);
public sealed record ConfirmEmailChangeRequest(string UserId, string Email, string Code);