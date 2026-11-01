using Hrms.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Notifications.Application.Abstractions;

public interface INotificationsDbContext
{
    DbSet<Notification> Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
