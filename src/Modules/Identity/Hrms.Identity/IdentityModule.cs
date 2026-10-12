using FluentValidation;
using Hrms.Identity.Application.Services;
using Hrms.Identity.Application.Validators;
using Hrms.Identity.Contracts;
using Hrms.Identity.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hrms.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddValidatorsFromAssemblyContaining<CreateUserModelValidator>();

        return services;
    }
}
