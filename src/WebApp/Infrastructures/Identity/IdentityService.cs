using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;


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

    private readonly IApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IUserContext _userContext;
    private readonly UrlEncoder _urlEncoder;
    private readonly ILogger<IdentityService> _logger;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment webHostEnvironment,
        IUserContext userContext,
        UrlEncoder urlEncoder,
        ILogger<IdentityService> logger,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        IApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _environment = webHostEnvironment;
        _userContext = userContext;
        _urlEncoder = urlEncoder;
        _logger = logger;
        _roleManager = roleManager;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public Result<AuthenticationProperties> CreateGoogleLoginProperties(string callbackUrl)
    {
        if (string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]) ||
            string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]))
            return Result<AuthenticationProperties>.Failure(AuthenticationErrors.GoogleNotConfigured);

        return Result<AuthenticationProperties>.Success(
            _signInManager.ConfigureExternalAuthenticationProperties(
                GoogleDefaults.AuthenticationScheme, callbackUrl));
    }

    public async Task<Result> LoginWithGoogleAsync(string? remoteError = null)
    {
        if (remoteError is not null)
            return await GoogleLoginFailureAsync("Login Google dibatalkan atau gagal.");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null || info.LoginProvider != GoogleDefaults.AuthenticationScheme)
            return await GoogleLoginFailureAsync("Sesi login Google tidak valid. Silakan coba lagi.");

        var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (user is null)
        {
            // Only trust the email returned by Google, never a posted email address.
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return await GoogleLoginFailureAsync("Google tidak memberikan alamat email.");

            if (await _userManager.FindByEmailAsync(email) is not null)
                return await GoogleLoginFailureAsync("Email sudah terdaftar. Gunakan metode login akun yang sudah ada; akun Google belum ditautkan.");

            if (!await _roleManager.RoleExistsAsync(AppRoles.Student))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(AppRoles.Student));
                if (!roleResult.Succeeded && !await _roleManager.RoleExistsAsync(AppRoles.Student))
                    return await GoogleLoginFailureAsync("Tidak dapat menyiapkan role Student.", roleResult);
            }

            user = new ApplicationUser
            {
                UserName = await CreateGoogleUsernameAsync(info.Principal),
                Email = email
            };
            
            var created = await _userManager.CreateAsync(user);
            if (!created.Succeeded)
                return await GoogleLoginFailureAsync("Tidak dapat membuat akun Student.", created);

            var assigned = await _userManager.AddToRoleAsync(user, AppRoles.Student);

            var linked = assigned.Succeeded ? await _userManager.AddLoginAsync(user, info) : assigned;
            if (!linked.Succeeded)
            {
                var cleanup = await _userManager.DeleteAsync(user);
                if (!cleanup.Succeeded)
                    _logger.LogError("Could not remove incomplete Google account {UserId}", user.Id);

                return await GoogleLoginFailureAsync("Tidak dapat menautkan akun Google. Silakan coba lagi.", linked);
            }
        }

        if (!await _userManager.IsInRoleAsync(user, AppRoles.Student))
            return await GoogleLoginFailureAsync("Akun ini tidak memiliki akses Student.");

        // Replace usernames generated by the old Google registration flow on next login.
        if (user.UserName is { } currentName && currentName.StartsWith("student_", StringComparison.Ordinal) &&
            currentName.Length == 40 && Guid.TryParseExact(currentName[8..], "N", out _))
        {
            var username = await CreateGoogleUsernameAsync(info.Principal);
            var updated = await _userManager.SetUserNameAsync(user, username);
            if (!updated.Succeeded)
                return await GoogleLoginFailureAsync("Tidak dapat memperbarui nama akun Google.", updated);
        }

        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, 
            info.ProviderKey, 
            isPersistent: false, 
            bypassTwoFactor: false);

        await _signInManager.Context.SignOutAsync(IdentityConstants.ExternalScheme);

        if (result.RequiresTwoFactor)
        {
            // A 2FA page is not available yet. Do not bypass this requirement.
            await _signInManager.Context.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);

            return GoogleLoginFailure("Akun memerlukan autentikasi dua faktor. Halaman verifikasi belum tersedia.");
        }
        if (!result.Succeeded)
            return GoogleLoginFailure(result.IsLockedOut
                ? "Akun sementara terkunci. Silakan coba lagi nanti."
                : "Akun belum diizinkan masuk.");

        _logger.LogInformation("Student {UserId} signed in with Google", user.Id);
        
        return Result.Success;
    }

    private async Task<string> CreateGoogleUsernameAsync(ClaimsPrincipal principal)
    {
        var googleName = principal.FindFirstValue(ClaimTypes.Name)?.Trim();
        if (string.IsNullOrWhiteSpace(googleName))
            googleName = principal.FindFirstValue(ClaimTypes.Email)?.Split('@')[0] ?? "Student";

        // Identity stores usernames in a column limited to 256 characters.
        var baseName = googleName.Length > 240 ? googleName[..240] : googleName;
        var username = baseName;
        for (var suffix = 2; await _userManager.FindByNameAsync(username) is not null; suffix++)
            username = $"{baseName} {suffix}";

        return username;
    }

    private static Result GoogleLoginFailure(string message) =>
        Result.Failure(AuthenticationErrors.GoogleLoginFailed(message));

    private async Task<Result> GoogleLoginFailureAsync(string message, IdentityResult? result = null)
    {
        if (result is not null)
            _logger.LogWarning(
                "Google account operation failed: {Codes}",
                string.Join(", ", result.Errors.Select(error => error.Code)));

        await _signInManager.Context.SignOutAsync(IdentityConstants.ExternalScheme);
        return GoogleLoginFailure(message);
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