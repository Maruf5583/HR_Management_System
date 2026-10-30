using FluentValidation;
using Hrms.Assets.Application.Abstractions;
using Hrms.Assets.Application.Services;
using Hrms.Assets.Application.Validators;
using Hrms.Assets.Contracts;
using Hrms.Assets.Infrastructure.Repositories;
using Hrms.Assets.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hrms.Assets;

public static class AssetsModule
{
    public static IServiceCollection AddAssetsModule(this IServiceCollection services)
    {
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAssetAssignmentRepository, AssetAssignmentRepository>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAssetLookup, AssetLookupService>();

        services.AddValidatorsFromAssemblyContaining<CreateAssetValidator>();

        return services;
    }
}
