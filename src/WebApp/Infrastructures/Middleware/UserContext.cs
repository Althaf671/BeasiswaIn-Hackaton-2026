using System.Net;
using System.Security.Claims;
using BeasiswaIn.Web.Interfaces;

namespace BeasiswaIn.Web.Infrastructuress.Middleware;

internal sealed class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor accessor)
    {
        _httpContextAccessor = accessor;
    }

    private HttpContext? Ctx => _httpContextAccessor.HttpContext;
    private ClaimsPrincipal? Principal => Ctx?.User;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? Username => Principal?.Identity?.Name;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];

    public IPAddress? IpAddress =>
        Ctx?.Connection.RemoteIpAddress is { } ip
            ? (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip)
            : null;

    public bool IsInRole(string role)
    {
        return Principal?.IsInRole(role) ?? false;
    } 
}