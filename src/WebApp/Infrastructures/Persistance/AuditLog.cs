

using System.Net;

namespace BeasiswaIn.Web.Infrastructures.Persistance;

internal sealed class AuditLog
{
    public string Id { get; init; } = Ulid.NewUlid().ToString();
    public string ActorUserId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public string? Description { get; init; }
    public IPAddress? IpAddress { get; init; }
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;

    public static AuditLog Create(
        string actorUserId, 
        string action, 
        string entityType,
        string? entityId = null, 
        string? description = null, 
        IPAddress? ipAddress = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        return new AuditLog
        {
            ActorUserId = actorUserId.Trim(),
            Action = action.Trim(),
            EntityType = entityType.Trim(),
            EntityId = entityId,
            Description = description?.Trim(),
            IpAddress = ipAddress,
        };
    }
}