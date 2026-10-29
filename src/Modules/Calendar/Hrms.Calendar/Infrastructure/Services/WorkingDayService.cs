using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Application.Services;
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
            if (day != null && day.IsWorkingDay != item.IsWorkingDay)
            {
                day.IsWorkingDay = item.IsWorkingDay;
                day.UpdatedAtUtc = now;
                day.UpdatedByUserId = userId;
                await _repository.UpdateAsync(day, cancellationToken);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
