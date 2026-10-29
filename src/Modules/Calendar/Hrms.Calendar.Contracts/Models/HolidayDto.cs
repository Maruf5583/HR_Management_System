namespace Hrms.Calendar.Contracts.Models;

public record HolidayDto(
    Guid HolidayId,
    string Name,
    DateOnly HolidayDate,
    string Type
);
