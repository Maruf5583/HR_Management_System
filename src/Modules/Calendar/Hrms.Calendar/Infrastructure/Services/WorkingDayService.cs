using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Application.Services;
using Hrms.Calendar.Domain.Entities;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;

namespace Hrms.Calendar.Infrastructure.Services;

public class WorkingDayService : IWorkingDayService
{
    private readonly ICompanyWorkingDayRepository _repository;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public WorkingDayService(
        ICompanyWorkingDayRepository repository,
        IClock clock,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CompanyWorkingDayDto>> GetWorkingDaysAsync(CancellationToken cancellationToken = default)
    {
        var days = await _repository.GetAllAsync(cancellationToken);

        // Auto-heal / initialize if table is empty
        if (days.Count == 0)
        {
            var now = _clock.UtcNow;
            var defaultDays = new (byte DayOfWeek, bool IsWorkingDay)[]
            {
                (0, true),  // Sunday - Working
                (1, true),  // Monday - Working
                (2, true),  // Tuesday - Working
                (3, true),  // Wednesday - Working
                (4, true),  // Thursday - Working
                (5, false), // Friday - Weekend off
                (6, false)  // Saturday - Weekend off
            };

            var toSeed = defaultDays.Select(d => new CompanyWorkingDay
            {
                CompanyWorkingDayId = Guid.NewGuid(),
                DayOfWeek = d.DayOfWeek,
                IsWorkingDay = d.IsWorkingDay,
                UpdatedAtUtc = now
            }).ToList();

            await _repository.AddRangeAsync(toSeed, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            days = await _repository.GetAllAsync(cancellationToken);
        }

        return days.Select(d => new CompanyWorkingDayDto(
            d.CompanyWorkingDayId,
            d.DayOfWeek,
            d.DayName,
            d.IsWorkingDay,
            d.UpdatedAtUtc,
            d.UpdatedByUserId
        )).ToList();
    }

    public async Task<Result> UpdateWorkingDaysAsync(UpdateWorkingDaysCommand command, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var item in command.Days)
        {
            var day = await _repository.GetByDayOfWeekAsync(item.DayOfWeek, cancellationToken);
            if (day != null)
            {
                if (day.IsWorkingDay != item.IsWorkingDay)
                {
                    day.IsWorkingDay = item.IsWorkingDay;
                    day.UpdatedAtUtc = now;
                    day.UpdatedByUserId = userId;
                    await _repository.UpdateAsync(day, cancellationToken);
                }
            }
            else
            {
                var newDay = new CompanyWorkingDay
                {
                    CompanyWorkingDayId = Guid.NewGuid(),
                    DayOfWeek = item.DayOfWeek,
                    IsWorkingDay = item.IsWorkingDay,
                    UpdatedAtUtc = now,
                    UpdatedByUserId = userId
                };
                await _repository.AddAsync(newDay, cancellationToken);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
