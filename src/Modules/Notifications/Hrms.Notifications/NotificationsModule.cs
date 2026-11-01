using Hrms.Notifications.Contracts;
using Hrms.Notifications.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hrms.Notifications;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<NotificationService>();
        services.AddScoped<INotificationSender>(sp => sp.GetRequiredService<NotificationService>());
        services.AddScoped<INotificationQuery>(sp => sp.GetRequiredService<NotificationService>());

        return services;
    }
}
