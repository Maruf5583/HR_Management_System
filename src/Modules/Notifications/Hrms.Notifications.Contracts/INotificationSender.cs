namespace Hrms.Notifications.Contracts;

public interface INotificationSender
{
    Task SendAsync(
        Guid userId,
        string title,
        string message,
        string? linkUrl = null,
        CancellationToken cancellationToken = default);
}
