namespace Hrms.Leave.Contracts;

public interface ILeaveQuery
{
    Task<bool> IsOnLeaveAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateOnly>> GetApprovedLeaveDatesAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<decimal> GetUnpaidLeaveDaysAsync(
        Guid employeeId,
        DateOnly monthStart,
        CancellationToken cancellationToken = default);
}
