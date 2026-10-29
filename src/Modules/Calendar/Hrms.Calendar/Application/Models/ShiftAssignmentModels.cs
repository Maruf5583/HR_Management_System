namespace Hrms.Calendar.Application.Models;

public record ShiftAssignmentDto(
    Guid ShiftAssignmentId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid ShiftId,
    string ShiftName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsCurrent,
    DateTime CreatedAtUtc
);

public record AssignShiftCommand(
    Guid EmployeeId,
    Guid ShiftId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null
);

public record TransferShiftCommand(
    Guid EmployeeId,
    Guid NewShiftId,
    DateOnly EffectiveFrom
);

public record EndShiftAssignmentCommand(
    Guid ShiftAssignmentId,
    DateOnly EffectiveTo
);
