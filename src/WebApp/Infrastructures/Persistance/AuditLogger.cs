using BeasiswaIn.Web.Interfaces;

namespace BeasiswaIn.Web.Infrastructures.Persistance;

internal sealed class AuditLogger : IAuditLogger
{
    private readonly IApplicationDbContext _db;             
    private readonly IUserContext _userContext;
    private readonly IHttpContextAccessor _http;

    public AuditLogger(
        IApplicationDbContext db, 
        IUserContext userContext, 
        IHttpContextAccessor http)
    {
        _db = db;
        _userContext = userContext;
        _http = http;
    }

    public void Log(
        string action, 
        string entityType, 
        string? entityId = null, 
        string? description = null)
    {
        var actor = _userContext.UserId ?? "anonymous"; 
        var ip = _http.HttpContext?.Connection.RemoteIpAddress;

        _db.AuditLogs.Add(
            AuditLog.Create(actor, action, entityType, entityId, description, ip));
    }
}