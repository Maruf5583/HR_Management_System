namespace Hrms.Calendar.Contracts.Models;

public record ShiftDetailsDto(
    Guid ShiftId,
    string ShiftName,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int GraceMinutes,
    int BreakMinutes,
    bool IsNightShift,
    Guid? ShiftVersionId = null
);
