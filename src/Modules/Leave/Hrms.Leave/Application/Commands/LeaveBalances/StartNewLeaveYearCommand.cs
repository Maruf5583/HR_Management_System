using Hrms.Employees.Contracts;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Application.Services;
using Hrms.Leave.Domain.Entities;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Commands.LeaveBalances;

public record StartNewLeaveYearCommand(int? Year);

public interface IStartNewLeaveYearHandler
{
    Task<Result<int>> HandleAsync(StartNewLeaveYearCommand command, CancellationToken cancellationToken = default);
}

public class StartNewLeaveYearHandler : IStartNewLeaveYearHandler
{
    private readonly ILeaveDbContext _context;
    private readonly ILeaveBalanceRepository _balanceRepository;
    private readonly ILeaveTypeRepository _typeRepository;
    private readonly IEmployeeLookup _employeeLookup;
    private readonly ILeaveYearHelper _yearHelper;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public StartNewLeaveYearHandler(
        ILeaveDbContext context,
        ILeaveBalanceRepository balanceRepository,
        ILeaveTypeRepository typeRepository,
        IEmployeeLookup employeeLookup,
        ILeaveYearHelper yearHelper,
        ICurrentUser currentUser,
        IClock clock)
    {
        _context = context;
        _balanceRepository = balanceRepository;
        _typeRepository = typeRepository;
        _employeeLookup = employeeLookup;
        _yearHelper = yearHelper;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<int>> HandleAsync(StartNewLeaveYearCommand command, CancellationToken cancellationToken = default)
    {
        int targetYear = command.Year ?? await _yearHelper.GetLeaveYearAsync(_clock.TodayLocal, cancellationToken);

        // 1. Fetch active leave types
        var activeTypes = await _typeRepository.GetAllAsync(activeOnly: true, cancellationToken);
        if (activeTypes.Count == 0)
        {
            return Result<int>.Failure("No active leave types found to generate balances.");
        }

        // 2. Fetch active employees
        var activeEmployees = await _employeeLookup.GetActiveEmployeesDropdownAsync(cancellationToken);
        if (activeEmployees.Count == 0)
        {
            return Result<int>.Failure("No active employees found to generate balances.");
        }

        var dbContext = (DbContext)_context;
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Fetch existing balances for targetYear to avoid duplicate key violations
                var existingBalances = await _context.LeaveBalances
                    .Where(b => b.Year == targetYear)
                    .Select(b => new { b.EmployeeId, b.LeaveTypeId })
                    .ToListAsync(cancellationToken);

                var existingSet = new HashSet<(Guid EmployeeId, Guid LeaveTypeId)>(
                    existingBalances.Select(b => (b.EmployeeId, b.LeaveTypeId)));

                var toAdd = new List<LeaveBalance>();
                var nowUtc = _clock.UtcNow;

                foreach (var emp in activeEmployees)
                {
                    foreach (var lt in activeTypes)
                    {
                        if (!existingSet.Contains((emp.EmployeeId, lt.LeaveTypeId)))
                        {
                            toAdd.Add(new LeaveBalance
                            {
                                LeaveBalanceId = Guid.NewGuid(),
                                EmployeeId = emp.EmployeeId,
                                LeaveTypeId = lt.LeaveTypeId,
                                Year = targetYear,
                                Entitled = lt.DaysPerYear,
                                Used = 0m,
                                UpdatedAtUtc = nowUtc,
                                UpdatedByUserId = _currentUser.UserId
                            });
                        }
                    }
                }

                if (toAdd.Count > 0)
                {
                    await _balanceRepository.AddRangeAsync(toAdd, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);
                return Result<int>.Success(toAdd.Count);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                return Result<int>.Failure($"Failed to initialize leave year: {ex.Message}");
            }
        });
    }
}
