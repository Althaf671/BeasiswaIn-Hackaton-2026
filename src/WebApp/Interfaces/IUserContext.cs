

using System.Net;

namespace BeasiswaIn.Web.Interfaces;

public interface IUserContext
{
    public string? UserId { get; }
    public string? Username { get; }
    public bool IsAuthenticated { get; }
    public IPAddress? IpAddress { get; }
    public IEnumerable<string> Roles { get; }
    public bool IsInRole(string role);
}