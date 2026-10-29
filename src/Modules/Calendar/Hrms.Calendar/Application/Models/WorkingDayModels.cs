namespace Hrms.Calendar.Application.Models;

public record CompanyWorkingDayDto(
    Guid CompanyWorkingDayId,
    byte DayOfWeek,
    string DayName,
    bool IsWorkingDay,
    DateTime UpdatedAtUtc,
    Guid? UpdatedByUserId
);

public record UpdateWorkingDayItem(
    byte DayOfWeek,
    bool IsWorkingDay
);

public record UpdateWorkingDaysCommand(
    IReadOnlyList<UpdateWorkingDayItem> Days
);
