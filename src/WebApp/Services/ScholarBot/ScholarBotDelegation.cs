using System.Security.Cryptography;
using System.Text;
using BeasiswaIn.Web.Infrastructures.Integrations.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace BeasiswaIn.Web.Services.ScholarBot;

internal sealed class ScholarBotDelegation
{
    internal const string ContextHeader = "X-ScholarBot-Context";
    private readonly ITimeLimitedDataProtector _protector;
    private readonly byte[] _serviceKeyHash;
    private readonly int _authorizationLength;
    private readonly TimeSpan _lifetime;

    public ScholarBotDelegation(IDataProtectionProvider protection, IOptions<ScholarBotOptions> options)
    {
        _protector = protection.CreateProtector("ScholarBot.UserDelegation.v1").ToTimeLimitedDataProtector();
        _serviceKeyHash = SHA256.HashData(Encoding.ASCII.GetBytes("Bearer " + options.Value.ServiceToken));
        _authorizationLength = "Bearer ".Length + options.Value.ServiceToken.Length;
        _lifetime = TimeSpan.FromSeconds(options.Value.TimeoutSeconds + 5);
    }

    internal string Create(string userId) => _protector.Protect(userId, _lifetime);

    internal bool HasServiceAuthorization(HttpRequest request)
    {
        var values = request.Headers.Authorization;
        if (values.Count != 1) return false;
        var authorization = values[0];
        if (authorization is null || authorization.Length != _authorizationLength ||
            !authorization.StartsWith("Bearer ", StringComparison.Ordinal)) return false;
        var token = authorization.AsSpan("Bearer ".Length);
        foreach (var character in token)
            if (!(character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_'))
                return false;
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.ASCII.GetBytes(authorization), hash);
        return CryptographicOperations.FixedTimeEquals(hash, _serviceKeyHash);
    }

    internal bool TryReadUser(HttpRequest request, out string userId)
    {
        userId = string.Empty;
        var values = request.Headers[ContextHeader];
        if (values.Count != 1 || values[0] is not { Length: > 0 and <= 4096 } token) return false;
        try
        {
            var candidate = _protector.Unprotect(token);
            if (!ScholarBotProtocol.IsValidText(candidate, 256)) return false;
            userId = candidate;
            return true;
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or ArgumentException)
        {
            return false;
        }
    }
}
