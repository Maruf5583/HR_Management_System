using FluentValidation;
using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Application.Services;
using Hrms.Employees.Infrastructure.Repositories;
using Hrms.Employees.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hrms.Employees;

public static class EmployeesModule
{
    public static IServiceCollection AddEmployeesModule(this IServiceCollection services)
    {
        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(EmployeesModule).Assembly);

        // Repositories
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDesignationRepository, DesignationRepository>();
        services.AddScoped<IGradeRepository, GradeRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        // Organization Services
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IDesignationService, DesignationService>();
        services.AddScoped<IGradeService, GradeService>();

        // Employee Services
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<Hrms.Employees.Contracts.IEmployeeLookup, EmployeeLookupService>();

        return services;
    }
}
