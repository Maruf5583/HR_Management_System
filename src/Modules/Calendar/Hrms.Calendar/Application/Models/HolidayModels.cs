using Hrms.Calendar.Domain.Enums;

namespace Hrms.Calendar.Application.Models;

public record HolidayDto(
    Guid HolidayId,
    string Name,
    DateOnly HolidayDate,
    HolidayType Type,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record CreateHolidayCommand(
    string Name,
    DateOnly HolidayDate,
    HolidayType Type
);

public record CreateMultiDayHolidayCommand(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    HolidayType Type
);

public record UpdateHolidayCommand(
    Guid HolidayId,
    string Name,
    DateOnly HolidayDate,
    HolidayType Type
);
