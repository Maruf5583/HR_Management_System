using FluentValidation;
using Hrms.Settings.Application.Services;
using Hrms.Settings.Contracts;
using Hrms.Settings.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hrms.Settings;

public static class SettingsModule
{
    public static IServiceCollection AddSettingsModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(SettingsModule).Assembly);

        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ISettingsReader, SettingsReader>();

        return services;
    }
}
