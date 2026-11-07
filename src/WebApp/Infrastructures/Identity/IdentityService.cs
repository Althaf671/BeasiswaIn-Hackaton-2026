

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using BeasiswaIn.Web.Common.Abstractions;
using BeasiswaIn.Web.Common.Errors;
using BeasiswaIn.Web.Common.Helpers;
using BeasiswaIn.Web.Infrastructures.Persistance;
using BeasiswaIn.Web.Interfaces;
using BeasiswaIn.Web.Models.Auth;

namespace BeasiswaIn.Web.Infrastructures.Identity;

internal sealed class IdentityService : IIdentityService
{
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
    private const string AuthenticatorIssuer = "BeasiswaIn";
    private const int RecoveryCodeCount = 10;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IUserContext _userContext;
    private readonly UrlEncoder _urlEncoder;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment webHostEnvironment,
        IUserContext userContext,
        UrlEncoder urlEncoder,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _environment = webHostEnvironment;
        _userContext = userContext;
        _urlEncoder = urlEncoder;
        _logger = logger;
    }

    public async Task<Result<GetMyAccountResponse>> GetMyAccountAsync()
    {
        var id = _userContext.UserId;
        if (string.IsNullOrWhiteSpace(id))
            return Result<GetMyAccountResponse>.Failure(AuthenticationErrors.InvalidCredentials);

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return Result<GetMyAccountResponse>.Failure(AuthenticationErrors.UserNotFound);

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count is 0)
            return Result<GetMyAccountResponse>.Failure(AuthenticationErrors.Forbidden);

        var res = new GetMyAccountResponse
        {
            Id = user.Id,
            UserName = StringHelper.ToProblemTitle(user.UserName ?? "unknown"),
            Email = user.Email ?? "-",
            Role = roles,
            Phone = user.PhoneNumber ?? "-",
            IsEmailConfirmed = user.EmailConfirmed,
            IsPhoneConfirmed = user.PhoneNumberConfirmed,
            IsTwoFactorEnabled = user.TwoFactorEnabled
        };

        return Result<GetMyAccountResponse>.Success(res);
    }

    public async Task<Result<string>> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            _userManager.PasswordHasher.HashPassword(null!, request.Password);
            return Result<string>.Failure(AuthenticationErrors.InvalidCredentials);
        }

        var signInRes = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (signInRes.IsLockedOut)
            return Result<string>.Failure(AuthenticationErrors.AccountLocked);

        // PageModel: redirect ke halaman LoginWith2fa saat error ini muncul.
        if (signInRes.RequiresTwoFactor)
            return Result<string>.Failure(AuthenticationErrors.RequiresTwoFactor);

        if (signInRes.IsNotAllowed)
            return Result<string>.Failure(AuthenticationErrors.UserNotAllowed);

        if (!signInRes.Succeeded)
            return Result<string>.Failure(AuthenticationErrors.InvalidCredentials);

        return Result<string>.Success(WelcomeMessage(user));
    }

    public async Task<Result<string>> LoginWith2faAsync(LoginWith2faRequest request)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
            return Result<string>.Failure(AuthenticationErrors.TwoFactorSessionInvalid);

        var authenticatorCode = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            authenticatorCode, request.RememberMe, request.RememberMachine);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} logged in with 2fa.", user.Id);
            return Result<string>.Success(WelcomeMessage(user));
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {UserId} account locked out.", user.Id);
            return Result<string>.Failure(AuthenticationErrors.AccountLocked);
        }

        _logger.LogWarning("Invalid authenticator code entered for user {UserId}.", user.Id);
        return Result<string>.Failure(AuthenticationErrors.InvalidAuthenticatorCode);
    }

    public async Task<Result<string>> LoginWithRecoveryCodeAsync(LoginWithRecoveryCodeRequest request)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
            return Result<string>.Failure(AuthenticationErrors.TwoFactorSessionInvalid);

        var recoveryCode = request.RecoveryCode.Replace(" ", string.Empty);

        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} logged in with a recovery code.", user.Id);
            return Result<string>.Success(WelcomeMessage(user));
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {UserId} account locked out.", user.Id);
            return Result<string>.Failure(AuthenticationErrors.AccountLocked);
        }

        _logger.LogWarning("Invalid recovery code entered for user {UserId}.", user.Id);
        return Result<string>.Failure(AuthenticationErrors.InvalidRecoveryCode);
    }

    public async Task<Result> LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return Result.Success;
    }

    public async Task<Result<TwoFactorStatusResponse>> GetTwoFactorStatusAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Result<TwoFactorStatusResponse>.Failure(AuthenticationErrors.Unauthorized);

        var res = new TwoFactorStatusResponse(
            HasAuthenticator: await _userManager.GetAuthenticatorKeyAsync(user) != null,
            Is2faEnabled: await _userManager.GetTwoFactorEnabledAsync(user),
            RecoveryCodesLeft: await _userManager.CountRecoveryCodesAsync(user));

        return Result<TwoFactorStatusResponse>.Success(res);
    }

    public async Task<Result<AuthenticatorSetupResponse>> GetAuthenticatorSetupAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Result<AuthenticatorSetupResponse>.Failure(AuthenticationErrors.Unauthorized);

        return Result<AuthenticatorSetupResponse>.Success(await LoadSharedKeyAndQrCodeUriAsync(user));
    }

    public async Task<Result<string[]>> EnableAuthenticatorAsync(EnableAuthenticatorRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Result<string[]>.Failure(AuthenticationErrors.Unauthorized);

        var verificationCode = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!is2faTokenValid)
            return Result<string[]>.Failure(AuthenticationErrors.InvalidVerificationCode);

        var enable = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!enable.Succeeded)
            return Result<string[]>.Failure(AuthenticationErrors.FailedToUpdateTwoFactor);

        await _signInManager.RefreshSignInAsync(user);

        _logger.LogInformation("User {UserId} has enabled 2FA with an authenticator app.", user.Id);

        if (await _userManager.CountRecoveryCodesAsync(user) == 0)
        {
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);
            return Result<string[]>.Success(recoveryCodes!.ToArray());
        }

        return Result<string[]>.Success(Array.Empty<string>());
    }

    public async Task<Result> DisableTwoFactorAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Result.Failure(AuthenticationErrors.Unauthorized);

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
            return Result.Failure(AuthenticationErrors.TwoFactorNotEnabled);

        var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disable2faResult.Succeeded)
            return Result.Failure(AuthenticationErrors.FailedToUpdateTwoFactor);

        await _signInManager.RefreshSignInAsync(user);

        _logger.LogInformation("User {UserId} has disabled 2fa.", user.Id);
        return Result.Success;
    }

    public async Task<Result<string>> RegisterUserAccountAsync(
        string username, 
        string password,
        string email, 
        string role,
        bool emailConfirmed)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
            return Result<string>.Failure(AuthenticationErrors.EmailAlreadyExists);

        var newUser = new ApplicationUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = emailConfirmed,
        };

        var passwordHash = _userManager.PasswordHasher.HashPassword(newUser, password);

        newUser.PasswordHash = passwordHash;

        var userRes = await _userManager.CreateAsync(newUser);
        if (!userRes.Succeeded)
        {
            var firstError = userRes.Errors.First();
            return _environment.IsDevelopment()
                ? Result<string>.Failure(AuthenticationErrors.FailedToCreateUserDueReasons with { Description = firstError.Description })
                : Result<string>.Failure(AuthenticationErrors.FailedToCreateUserDueReasons);
        }

        var roleRes = await _userManager.AddToRoleAsync(newUser, role);
        if (!roleRes.Succeeded)
        {
            var firstError = userRes.Errors.First();
            return _environment.IsDevelopment()
                ? Result<string>.Failure(AuthorizationErrors.FailedToAssignRole with { Description = firstError.Description })
                : Result<string>.Failure(AuthorizationErrors.FailedToAssignRole);
        }

        return Result<string>.Success(newUser.Id);
    }

    public async Task<Result> ResetAuthenticatorAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Result.Failure(AuthenticationErrors.Unauthorized);

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        _logger.LogInformation("User {UserId} has reset their authentication app key.", user.Id);

        await _signInManager.RefreshSignInAsync(user);
        return Result.Success;
    }

    public async Task<Result<string[]>> GenerateRecoveryCodesAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Result<string[]>.Failure(AuthenticationErrors.Unauthorized);

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
            return Result<string[]>.Failure(AuthenticationErrors.TwoFactorNotEnabled);

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);
        _logger.LogInformation("User {UserId} has generated new 2FA recovery codes.", user.Id);

        return Result<string[]>.Success(recoveryCodes!.ToArray());
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Result.Failure(AuthenticationErrors.Unauthorized);

        var changePasswordResult = await _userManager.ChangePasswordAsync(
            user, request.OldPassword, request.NewPassword);

        if (!changePasswordResult.Succeeded)
            return Result.Failure(ToPasswordError(changePasswordResult.Errors));

        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("User {UserId} changed their password successfully.", user.Id);

        return Result.Success;
    }

    public async Task<Result> SetPhoneNumberAsync(SetPhoneNumberRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Result.Failure(AuthenticationErrors.Unauthorized);

        var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
        if (request.PhoneNumber != phoneNumber)
        {
            var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
            if (!setPhoneResult.Succeeded)
                return Result.Failure(AuthenticationErrors.FailedToSetPhoneNumber);
        }

        await _signInManager.RefreshSignInAsync(user);
        return Result.Success;
    }

    public async Task<Result<string>> RequestEmailChangeAsync(string newEmail)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Result<string>.Failure(AuthenticationErrors.Unauthorized);

        var email = await _userManager.GetEmailAsync(user);
        if (string.Equals(newEmail, email, StringComparison.OrdinalIgnoreCase))
            return Result<string>.Failure(AuthenticationErrors.EmailUnchanged);

        var code = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        return Result<string>.Success(code);
    }

    public async Task<Result> ConfirmEmailChangeAsync(ConfirmEmailChangeRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return Result.Failure(AuthenticationErrors.UserNotFound);

        string code;
        try
        {
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
        }
        catch (FormatException)
        {
            return Result.Failure(AuthenticationErrors.InvalidEmailChangeToken);
        }

        var usernameFollowsEmail = string.Equals(user.UserName, user.Email, StringComparison.OrdinalIgnoreCase);

        var result = await _userManager.ChangeEmailAsync(user, request.Email, code);
        if (!result.Succeeded)
        {
            return Result.Failure(
                result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateEmail))
                    ? AuthenticationErrors.EmailAlreadyInUse
                    : AuthenticationErrors.FailedToChangeEmail);
        }

        if (usernameFollowsEmail)
        {
            var setUserNameResult = await _userManager.SetUserNameAsync(user, request.Email);
            if (!setUserNameResult.Succeeded)
                return Result.Failure(AuthenticationErrors.FailedToChangeEmail);
        }

        if (_userContext.IsAuthenticated && _userContext.UserId == user.Id)
            await _signInManager.RefreshSignInAsync(user);

        return Result.Success;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var id = _userContext.UserId;
        return string.IsNullOrWhiteSpace(id) ? null : await _userManager.FindByIdAsync(id);
    }

    private static string WelcomeMessage(ApplicationUser user) =>
        $"Selamat datang kembali {StringHelper.ToProblemTitle(user.UserName ?? "unknown")}.";

    private static Error ToPasswordError(IEnumerable<IdentityError> errors)
    {
        var codes = errors.Select(e => e.Code).ToHashSet();

        if (codes.Contains(nameof(IdentityErrorDescriber.PasswordMismatch)))
            return AuthenticationErrors.InvalidOldPassword;

        var reasons = new List<string>();

        if (codes.Contains(nameof(IdentityErrorDescriber.PasswordTooShort)))
            reasons.Add(AuthenticationErrors.PasswordTooShort);

        if (codes.Overlaps(new[]
            {
                nameof(IdentityErrorDescriber.PasswordRequiresUpper),
                nameof(IdentityErrorDescriber.PasswordRequiresLower),
                nameof(IdentityErrorDescriber.PasswordRequiresDigit),
                nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric)
            }))
            reasons.Add(AuthenticationErrors.ComplexityRequirementsMessage);

        return AuthenticationErrors.FailedToChangePassword(
            reasons.Count > 0 ? string.Join(" ", reasons) : "Gagal mengubah kata sandi.");
    }

    private async Task<AuthenticatorSetupResponse> LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
    {
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = await _userManager.GetEmailAsync(user);

        return new AuthenticatorSetupResponse(
            SharedKey: FormatKey(unformattedKey!),
            AuthenticatorUri: GenerateQrCodeUri(email!, unformattedKey!));
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            _urlEncoder.Encode(AuthenticatorIssuer),
            _urlEncoder.Encode(email),
            unformattedKey);
    }
}