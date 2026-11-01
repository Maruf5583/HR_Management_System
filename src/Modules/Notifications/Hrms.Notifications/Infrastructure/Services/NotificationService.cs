using Hrms.Notifications.Application.Abstractions;
using Hrms.Notifications.Contracts;
using Hrms.Notifications.Contracts.Models;
using Hrms.Notifications.Domain.Entities;
using Hrms.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Notifications.Infrastructure.Services;

public class NotificationService : INotificationSender, INotificationQuery
{
    private readonly INotificationsDbContext _context;
    private readonly IClock _clock;

    public NotificationService(INotificationsDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task SendAsync(
        Guid userId,
        string title,
        string message,
        string? linkUrl = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            LinkUrl = linkUrl,
            IsRead = false,
            CreatedAtUtc = _clock.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var list = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return list.Select(n => new NotificationDto(
            n.NotificationId,
            n.UserId,
            n.Title,
            n.Message,
            n.LinkUrl,
            n.IsRead,
            n.CreatedAtUtc,
            GetTimeAgo(now - n.CreatedAtUtc)
        )).ToList();
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId, cancellationToken);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = _clock.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count > 0)
        {
            var now = _clock.UtcNow;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAtUtc = now;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GetTimeAgo(TimeSpan span)
    {
        if (span.TotalSeconds < 60) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
        return $"{(int)(span.TotalDays / 30)}mo ago";
    }
}
