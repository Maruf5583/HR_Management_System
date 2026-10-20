using System.Text.Json;
using FluentValidation;
using Hrms.Audit.Contracts;
using Hrms.Audit.Contracts.Models;
using Hrms.Settings.Application.Abstractions;
using Hrms.Settings.Application.Models;
using Hrms.Settings.Application.Services;
using Hrms.Settings.Contracts;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Settings.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly ISettingsDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IValidator<UpdateSettingsCommand> _validator;
    private readonly IAuditWriter? _auditWriter;

    public SettingsService(
        ISettingsDbContext context,
        ICurrentUser currentUser,
        IClock clock,
        IValidator<UpdateSettingsCommand> validator,
        IAuditWriter? auditWriter = null)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _validator = validator;
        _auditWriter = auditWriter;
    }

    public async Task<IReadOnlyList<SettingDto>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Settings
            .AsNoTracking()
            .Select(s => new SettingDto(
                s.SettingId,
                s.SettingKey,
                s.ValueText,
                s.Description,
                s.UpdatedAtUtc,
                s.UpdatedByUserId
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result> UpdateSettingsAsync(UpdateSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(new Error("Validation.Error", validation.Errors[0].ErrorMessage));
        }

        var newValues = new Dictionary<string, string>
        {
            [SettingKeys.CompanyName] = command.CompanyName.Trim(),
            [SettingKeys.CompanyTimezone] = command.CompanyTimezone.Trim(),
            [SettingKeys.AttendanceHalfDayHours] = command.AttendanceHalfDayHours.Trim(),
            [SettingKeys.AttendanceHolidayPriority] = command.AttendanceHolidayPriority.Trim(),
            [SettingKeys.LeaveYearStart] = command.LeaveYearStart.Trim(),
            [SettingKeys.PayrollPerDayDivisor] = command.PayrollPerDayDivisor.Trim(),
            [SettingKeys.EmployeeCodeFormat] = command.EmployeeCodeFormat.Trim()
        };

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var existingSettings = await _context.Settings.ToListAsync(cancellationToken);
        var now = _clock.UtcNow;
        var currentUserId = _currentUser.UserId;

        var oldValuesDict = new Dictionary<string, string>();
        var newValuesDict = new Dictionary<string, string>();

        foreach (var setting in existingSettings)
        {
            if (newValues.TryGetValue(setting.SettingKey, out var newValue))
            {
                if (setting.ValueText != newValue)
                {
                    oldValuesDict[setting.SettingKey] = setting.ValueText;
                    newValuesDict[setting.SettingKey] = newValue;

                    setting.ValueText = newValue;
                    setting.UpdatedAtUtc = now;
                    setting.UpdatedByUserId = currentUserId;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (oldValuesDict.Count > 0 && _auditWriter != null)
        {
            await _auditWriter.WriteAsync(new AuditLogEntry(
                Action: "UPDATE",
                EntityName: "Settings",
                EntityId: "Global",
                OldValues: JsonSerializer.Serialize(oldValuesDict),
                NewValues: JsonSerializer.Serialize(newValuesDict),
                UserId: currentUserId
            ), cancellationToken);
        }

        return Result.Success();
    }
}
