using FluentValidation;
using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Application.Services;
using Hrms.Calendar.Domain.Entities;
using Hrms.Calendar.Domain.Enums;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;

namespace Hrms.Calendar.Infrastructure.Services;

public class HolidayService : IHolidayService
{
    private readonly IHolidayRepository _holidayRepository;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateHolidayCommand> _createValidator;
    private readonly IValidator<CreateMultiDayHolidayCommand> _multiDayValidator;

    public HolidayService(
        IHolidayRepository holidayRepository,
        IClock clock,
        ICurrentUser currentUser,
        IValidator<CreateHolidayCommand> createValidator,
        IValidator<CreateMultiDayHolidayCommand> multiDayValidator)
    {
        _holidayRepository = holidayRepository;
        _clock = clock;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _multiDayValidator = multiDayValidator;
    }

    public async Task<IReadOnlyList<HolidayDto>> GetHolidaysAsync(int? year = null, HolidayType? type = null, CancellationToken cancellationToken = default)
    {
        var holidays = await _holidayRepository.GetAllAsync(year, type, cancellationToken);
        return holidays.Select(h => new HolidayDto(
            h.HolidayId,
            h.Name,
            h.HolidayDate,
            h.Type,
            h.CreatedAtUtc,
            h.UpdatedAtUtc
        )).ToList();
    }

    public async Task<IReadOnlyList<HolidayDto>> GetHolidaysForRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var holidays = await _holidayRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        return holidays.Select(h => new HolidayDto(
            h.HolidayId,
            h.Name,
            h.HolidayDate,
            h.Type,
            h.CreatedAtUtc,
            h.UpdatedAtUtc
        )).ToList();
    }

    public async Task<HolidayDto?> GetByIdAsync(Guid holidayId, CancellationToken cancellationToken = default)
    {
        var holiday = await _holidayRepository.GetByIdAsync(holidayId, cancellationToken);
        if (holiday == null) return null;

        return new HolidayDto(
            holiday.HolidayId,
            holiday.Name,
            holiday.HolidayDate,
            holiday.Type,
            holiday.CreatedAtUtc,
            holiday.UpdatedAtUtc
        );
    }

    public async Task<Result<Guid>> CreateHolidayAsync(CreateHolidayCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<Guid>.Failure(Error.Validation(validation.Errors[0].ErrorMessage));
        }

        var exists = await _holidayRepository.ExistsOnDateAsync(command.HolidayDate, cancellationToken: cancellationToken);
        if (exists)
        {
            return Result<Guid>.Failure(Error.Conflict($"A holiday is already scheduled on {command.HolidayDate:yyyy-MM-dd}."));
        }

        var holiday = new Holiday
        {
            HolidayId = Guid.NewGuid(),
            Name = command.Name.Trim(),
            HolidayDate = command.HolidayDate,
            Type = command.Type,
            CreatedAtUtc = _clock.UtcNow,
            CreatedByUserId = _currentUser.UserId
        };

        await _holidayRepository.AddAsync(holiday, cancellationToken);
        await _holidayRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(holiday.HolidayId);
    }

    public async Task<Result<IReadOnlyList<Guid>>> CreateMultiDayHolidayAsync(CreateMultiDayHolidayCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _multiDayValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<IReadOnlyList<Guid>>.Failure(Error.Validation(validation.Errors[0].ErrorMessage));
        }

        var existingHolidays = await _holidayRepository.GetByDateRangeAsync(command.StartDate, command.EndDate, cancellationToken);
        if (existingHolidays.Count > 0)
        {
            var conflictDates = string.Join(", ", existingHolidays.Select(h => h.HolidayDate.ToString("yyyy-MM-dd")));
            return Result<IReadOnlyList<Guid>>.Failure(Error.Conflict($"Conflict detected: Holidays already exist on the following dates in the selected range: {conflictDates}."));
        }

        var holidays = new List<Holiday>();
        var currentDate = command.StartDate;

        while (currentDate <= command.EndDate)
        {
            holidays.Add(new Holiday
            {
                HolidayId = Guid.NewGuid(),
                Name = command.Name.Trim(),
                HolidayDate = currentDate,
                Type = command.Type,
                CreatedAtUtc = _clock.UtcNow,
                CreatedByUserId = _currentUser.UserId
            });

            currentDate = currentDate.AddDays(1);
        }

        await _holidayRepository.AddRangeAsync(holidays, cancellationToken);
        await _holidayRepository.SaveChangesAsync(cancellationToken);

        IReadOnlyList<Guid> ids = holidays.Select(h => h.HolidayId).ToList();
        return Result<IReadOnlyList<Guid>>.Success(ids);
    }

    public async Task<Result> UpdateHolidayAsync(UpdateHolidayCommand command, CancellationToken cancellationToken = default)
    {
        var holiday = await _holidayRepository.GetByIdAsync(command.HolidayId, cancellationToken);
        if (holiday == null)
        {
            return Result.Failure(Error.NotFound("Holiday not found."));
        }

        if (holiday.HolidayDate != command.HolidayDate)
        {
            var exists = await _holidayRepository.ExistsOnDateAsync(command.HolidayDate, command.HolidayId, cancellationToken);
            if (exists)
            {
                return Result.Failure(Error.Conflict($"A holiday already exists on {command.HolidayDate:yyyy-MM-dd}."));
            }
        }

        holiday.Name = command.Name.Trim();
        holiday.HolidayDate = command.HolidayDate;
        holiday.Type = command.Type;
        holiday.UpdatedAtUtc = _clock.UtcNow;
        holiday.UpdatedByUserId = _currentUser.UserId;

        await _holidayRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteHolidayAsync(Guid holidayId, CancellationToken cancellationToken = default)
    {
        var holiday = await _holidayRepository.GetByIdAsync(holidayId, cancellationToken);
        if (holiday == null)
        {
            return Result.Failure(Error.NotFound("Holiday not found."));
        }

        await _holidayRepository.DeleteAsync(holiday, cancellationToken);
        await _holidayRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<int>> DeleteHolidaysMultipleAsync(IEnumerable<Guid> holidayIds, CancellationToken cancellationToken = default)
    {
        var idList = holidayIds.Distinct().ToList();
        if (!idList.Any())
        {
            return Result<int>.Failure(Error.Validation("No holidays selected for deletion."));
        }

        var toDelete = new List<Holiday>();
        foreach (var id in idList)
        {
            var h = await _holidayRepository.GetByIdAsync(id, cancellationToken);
            if (h != null)
            {
                toDelete.Add(h);
            }
        }

        if (!toDelete.Any())
        {
            return Result<int>.Failure(Error.NotFound("None of the selected holidays were found."));
        }

        await _holidayRepository.DeleteRangeAsync(toDelete, cancellationToken);
        await _holidayRepository.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(toDelete.Count);
    }
}
