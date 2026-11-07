namespace BeasiswaIn.Web.Interfaces;

public interface IAuditLogger
{
    void Log(string action, string entityType, string? entityId = null, string? description = null);
}