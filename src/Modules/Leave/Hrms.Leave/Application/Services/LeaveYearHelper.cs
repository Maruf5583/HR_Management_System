using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Domain.Entities;
using Hrms.Settings.Contracts;

namespace Hrms.Leave.Application.Services;

public interface ILeaveYearHelper
{
    Task<int> GetLeaveYearAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<LeaveBalance> GetOrCreateBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default);
}

public class LeaveYearHelper : ILeaveYearHelper
{
    private readonly ISettingsReader _settingsReader;
    private readonly ILeaveBalanceRepository _balanceRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public LeaveYearHelper(
        ISettingsReader settingsReader,
        ILeaveBalanceRepository balanceRepository,
        ILeaveTypeRepository leaveTypeRepository)
    {
        _settingsReader = settingsReader;
        _balanceRepository = balanceRepository;
        _leaveTypeRepository = leaveTypeRepository;
    }

    public async Task<int> GetLeaveYearAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var yearStartStr = await _settingsReader.GetStringAsync(SettingKeys.LeaveYearStart, cancellationToken);
        if (string.IsNullOrWhiteSpace(yearStartStr))
        {
            yearStartStr = "01-01";
        }

        // yearStartStr format: MM-dd (e.g. "01-01")
        var parts = yearStartStr.Split('-');
        int month = 1;
        int day = 1;
        if (parts.Length == 2 && int.TryParse(parts[0], out var m) && int.TryParse(parts[1], out var d))
        {
            month = m;
            day = d;
        }

        var startInCurrentYear = new DateOnly(date.Year, month, day);
        if (date >= startInCurrentYear)
        {
            return date.Year;
        }

        return date.Year - 1;
    }

    public async Task<LeaveBalance> GetOrCreateBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var balance = await _balanceRepository.GetByEmployeeTypeYearAsync(employeeId, leaveTypeId, year, cancellationToken);
        if (balance != null)
        {
            return balance;
        }

        var leaveType = await _leaveTypeRepository.GetByIdAsync(leaveTypeId, cancellationToken);
        var entitled = leaveType?.DaysPerYear ?? 0m;

        balance = LeaveBalance.Create(employeeId, leaveTypeId, year, entitled, currentUserId);
        await _balanceRepository.AddAsync(balance, cancellationToken);

        return balance;
    }
}
