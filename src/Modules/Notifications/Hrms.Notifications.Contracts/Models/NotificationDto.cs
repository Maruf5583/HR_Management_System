namespace Hrms.Notifications.Contracts.Models;

public record NotificationDto(
    Guid NotificationId,
    Guid UserId,
    string Title,
    string Message,
    string? LinkUrl,
    bool IsRead,
    DateTime CreatedAtUtc,
    string TimeAgo
);
