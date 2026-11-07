using BeasiswaIn.Web.Common.Enums;

namespace BeasiswaIn.Web.Entities;

internal sealed class Notification
{
    public string Id { get; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; } 
    public NotificationStatus Status { get; set; }
    public string? ActionUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string AppUserId { get; set; } = string.Empty;

    public Notification(
        string id,
        string title,
        string content,
        NotificationType type,
        NotificationStatus status,
        string? actionUrl,
        DateTimeOffset createdAt,
        string appUserId)
    {
        Id = id;
        Title = title;
        Content = content;
        Type = type;
        Status = status;
        ActionUrl = actionUrl;
        AppUserId = appUserId;
        CreatedAt = createdAt;
    }
}