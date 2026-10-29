using Hrms.Calendar.Domain.Enums;

namespace Hrms.Calendar.Application.Models;

public record ShiftDto(
    Guid ShiftId,
    string Name,
    ShiftStatus Status,
    ShiftVersionDto? CurrentVersion,
    int VersionCount,
    int ActiveEmployeeCount,
    DateTime CreatedAtUtc
);

public record ShiftVersionDto(
    Guid ShiftVersionId,
    Guid ShiftId,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int GraceMinutes,
    int BreakMinutes,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsNightShift,
    TimeSpan TotalShiftSpan,
    TimeSpan NetWorkingSpan,
    DateTime CreatedAtUtc
);

public record ShiftDetailsDto(
    Guid ShiftId,
    string Name,
    ShiftStatus Status,
    IReadOnlyList<ShiftVersionDto> Versions,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record CreateShiftCommand(
    string Name,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int GraceMinutes,
    int BreakMinutes,
    DateOnly EffectiveFrom
);

public record CreateShiftVersionCommand(
    Guid ShiftId,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int GraceMinutes,
    int BreakMinutes,
    DateOnly EffectiveFrom
);

public record UpdateShiftStatusCommand(
    Guid ShiftId,
    ShiftStatus Status
);
