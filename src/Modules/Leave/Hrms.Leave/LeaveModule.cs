using FluentValidation;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Application.Commands.Approvals;
using Hrms.Leave.Application.Commands.LeaveBalances;
using Hrms.Leave.Application.Commands.LeaveRecords;
using Hrms.Leave.Application.Commands.LeaveRequests;
using Hrms.Leave.Application.Commands.LeaveTypes;
using Hrms.Leave.Application.Queries.Approvals;
using Hrms.Leave.Application.Queries.LeaveBalances;
using Hrms.Leave.Application.Queries.LeaveRecords;
using Hrms.Leave.Application.Queries.LeaveTypes;
using Hrms.Leave.Application.Queries.MyLeave;
using Hrms.Leave.Application.Services;
using Hrms.Leave.Contracts;
using Hrms.Leave.Infrastructure.Repositories;
using Hrms.Leave.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hrms.Leave;

public static class LeaveModule
{
    public static IServiceCollection AddLeaveModule(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
        services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();
        services.AddScoped<ILeaveRecordRepository, LeaveRecordRepository>();

        // Domain & Application Services
        services.AddScoped<ILeaveYearHelper, LeaveYearHelper>();
        services.AddScoped<ILeaveDaysCalculator, LeaveDaysCalculator>();
        services.AddScoped<ILeaveQuery, LeaveQueryService>();

        // Handlers
        services.AddScoped<ICreateLeaveTypeHandler, CreateLeaveTypeHandler>();
        services.AddScoped<IUpdateLeaveTypeHandler, UpdateLeaveTypeHandler>();
        services.AddScoped<IToggleLeaveTypeStatusHandler, ToggleLeaveTypeStatusHandler>();
        services.AddScoped<IGetLeaveTypesHandler, GetLeaveTypesHandler>();
        services.AddScoped<IStartNewLeaveYearHandler, StartNewLeaveYearHandler>();
        services.AddScoped<IGetLeaveBalancesHandler, GetLeaveBalancesHandler>();
        services.AddScoped<IRecordDirectLeaveHandler, RecordDirectLeaveHandler>();
        services.AddScoped<ICancelApprovedLeaveHandler, CancelApprovedLeaveHandler>();
        services.AddScoped<IApplyLeaveHandler, ApplyLeaveHandler>();
        services.AddScoped<ICancelPendingLeaveHandler, CancelPendingLeaveHandler>();
        services.AddScoped<IApproveLeaveRequestHandler, ApproveLeaveRequestHandler>();
        services.AddScoped<IRejectLeaveRequestHandler, RejectLeaveRequestHandler>();
        services.AddScoped<IGetApprovalsHandler, GetApprovalsHandler>();
        services.AddScoped<IGetMyLeaveSummaryHandler, GetMyLeaveSummaryHandler>();
        services.AddScoped<IGetLeaveRecordsHandler, GetLeaveRecordsHandler>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<ApplyLeaveCommandValidator>();
        services.AddValidatorsFromAssemblyContaining<RecordDirectLeaveCommandValidator>();
        services.AddValidatorsFromAssemblyContaining<CreateLeaveTypeCommandValidator>();
        services.AddValidatorsFromAssemblyContaining<RejectLeaveRequestCommandValidator>();

        return services;
    }
}
