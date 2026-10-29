using FluentValidation;
using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Application.Services;
using Hrms.Calendar.Domain.Entities;
using Hrms.Calendar.Domain.Enums;
using Hrms.Employees.Contracts;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;

namespace Hrms.Calendar.Infrastructure.Services;

public class ShiftService : IShiftService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftAssignmentRepository _assignmentRepository;
    private readonly IEmployeeLookup _employeeLookup;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateShiftCommand> _createShiftValidator;
    private readonly IValidator<CreateShiftVersionCommand> _createVersionValidator;
    private readonly IValidator<AssignShiftCommand> _assignValidator;

    public ShiftService(
        IShiftRepository shiftRepository,
        IShiftAssignmentRepository assignmentRepository,
        IEmployeeLookup employeeLookup,
        IClock clock,
        ICurrentUser currentUser,
        IValidator<CreateShiftCommand> createShiftValidator,
        IValidator<CreateShiftVersionCommand> createVersionValidator,
        IValidator<AssignShiftCommand> assignValidator)
    {
        _shiftRepository = shiftRepository;
        _assignmentRepository = assignmentRepository;
        _employeeLookup = employeeLookup;
        _clock = clock;
        _currentUser = currentUser;
        _createShiftValidator = createShiftValidator;
        _createVersionValidator = createVersionValidator;
        _assignValidator = assignValidator;
    }

    public async Task<IReadOnlyList<ShiftDto>> GetShiftsAsync(ShiftStatus? status = null, CancellationToken cancellationToken = default)
    {
        var shifts = await _shiftRepository.GetAllAsync(status, cancellationToken);
        var today = DateOnly.FromDateTime(_clock.UtcNow);

        return shifts.Select(s =>
        {
            var currentVersion = s.CurrentVersion ?? s.Versions.OrderByDescending(v => v.EffectiveFrom).FirstOrDefault();
            ShiftVersionDto? versionDto = null;

            if (currentVersion != null)
            {
                versionDto = new ShiftVersionDto(
                    currentVersion.ShiftVersionId,
                    currentVersion.ShiftId,
                    currentVersion.StartTime,
                    currentVersion.EndTime,
                    currentVersion.GraceMinutes,
                    currentVersion.BreakMinutes,
                    currentVersion.EffectiveFrom,
                    currentVersion.EffectiveTo,
                    currentVersion.IsNightShift,
                    currentVersion.TotalShiftSpan,
                    currentVersion.NetWorkingSpan,
                    currentVersion.CreatedAtUtc
                );
            }

            var activeEmployees = s.Assignments.Count(a => a.IsActive(today));

            return new ShiftDto(
                s.ShiftId,
                s.Name,
                s.Status,
                versionDto,
                s.Versions.Count,
                activeEmployees,
                s.CreatedAtUtc
            );
        }).ToList();
    }

    public async Task<ShiftDetailsDto?> GetShiftDetailsAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.GetByIdWithVersionsAsync(shiftId, cancellationToken);
        if (shift == null) return null;

        var versions = shift.Versions.Select(v => new ShiftVersionDto(
            v.ShiftVersionId,
            v.ShiftId,
            v.StartTime,
            v.EndTime,
            v.GraceMinutes,
            v.BreakMinutes,
            v.EffectiveFrom,
            v.EffectiveTo,
            v.IsNightShift,
            v.TotalShiftSpan,
            v.NetWorkingSpan,
            v.CreatedAtUtc
        )).ToList();

        return new ShiftDetailsDto(
            shift.ShiftId,
            shift.Name,
            shift.Status,
            versions,
            shift.CreatedAtUtc,
            shift.UpdatedAtUtc
        );
    }

    public async Task<Result<Guid>> CreateShiftAsync(CreateShiftCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _createShiftValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<Guid>.Failure(Error.Validation(validation.Errors[0].ErrorMessage));
        }

        var exists = await _shiftRepository.ExistsByNameAsync(command.Name, cancellationToken: cancellationToken);
        if (exists)
        {
            return Result<Guid>.Failure(Error.Conflict($"A shift with name '{command.Name}' already exists."));
        }

        var shift = new Shift
        {
            ShiftId = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Status = ShiftStatus.Active,
            CreatedAtUtc = _clock.UtcNow,
            CreatedByUserId = _currentUser.UserId
        };

        var initialVersion = new ShiftVersion
        {
            ShiftVersionId = Guid.NewGuid(),
            ShiftId = shift.ShiftId,
            StartTime = command.StartTime,
            EndTime = command.EndTime,
            GraceMinutes = command.GraceMinutes,
            BreakMinutes = command.BreakMinutes,
            EffectiveFrom = command.EffectiveFrom,
            EffectiveTo = null,
            ChangedByUserId = _currentUser.UserId,
            CreatedAtUtc = _clock.UtcNow
        };

        shift.Versions.Add(initialVersion);

        await _shiftRepository.AddAsync(shift, cancellationToken);
        await _shiftRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(shift.ShiftId);
    }

    public async Task<Result> CreateShiftVersionAsync(CreateShiftVersionCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _createVersionValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(Error.Validation(validation.Errors[0].ErrorMessage));
        }

        var shift = await _shiftRepository.GetByIdAsync(command.ShiftId, cancellationToken);
        if (shift == null)
        {
            return Result.Failure(Error.NotFound("Shift not found."));
        }

        if (shift.Status != ShiftStatus.Active)
        {
            return Result.Failure(Error.Failure("Cannot add a new timing version to an inactive shift."));
        }

        var latestVersion = await _shiftRepository.GetLatestVersionAsync(command.ShiftId, cancellationToken);
        if (latestVersion != null)
        {
            if (command.EffectiveFrom <= latestVersion.EffectiveFrom)
            {
                return Result.Failure(Error.Failure($"New version effective date ({command.EffectiveFrom:yyyy-MM-dd}) must be strictly after the previous version start date ({latestVersion.EffectiveFrom:yyyy-MM-dd})."));
            }

            if (latestVersion.EffectiveTo == null)
            {
                latestVersion.EffectiveTo = command.EffectiveFrom.AddDays(-1);
            }
        }

        var newVersion = new ShiftVersion
        {
            ShiftVersionId = Guid.NewGuid(),
            ShiftId = command.ShiftId,
            StartTime = command.StartTime,
            EndTime = command.EndTime,
            GraceMinutes = command.GraceMinutes,
            BreakMinutes = command.BreakMinutes,
            EffectiveFrom = command.EffectiveFrom,
            EffectiveTo = null,
            ChangedByUserId = _currentUser.UserId,
            CreatedAtUtc = _clock.UtcNow
        };

        await _shiftRepository.AddVersionAsync(newVersion, cancellationToken);
        await _shiftRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetShiftStatusAsync(Guid shiftId, ShiftStatus status, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        if (shift == null)
        {
            return Result.Failure(Error.NotFound("Shift not found."));
        }

        if (shift.Status != status)
        {
            shift.Status = status;
            shift.UpdatedAtUtc = _clock.UtcNow;
            shift.UpdatedByUserId = _currentUser.UserId;
            await _shiftRepository.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> UpdateShiftNameAsync(Guid shiftId, string newName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(Error.Validation("Shift name is required."));
        }

        var shift = await _shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        if (shift == null)
        {
            return Result.Failure(Error.NotFound("Shift not found."));
        }

        var trimmedName = newName.Trim();
        var exists = await _shiftRepository.ExistsByNameAsync(trimmedName, excludeShiftId: shiftId, cancellationToken: cancellationToken);
        if (exists)
        {
            return Result.Failure(Error.Conflict($"A shift with name '{trimmedName}' already exists."));
        }

        shift.Name = trimmedName;
        shift.UpdatedAtUtc = _clock.UtcNow;
        shift.UpdatedByUserId = _currentUser.UserId;
        await _shiftRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteShiftAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        if (shift == null)
        {
            return Result.Failure(Error.NotFound("Shift not found."));
        }

        var hasAssignments = await _assignmentRepository.GetAssignmentsForShiftAsync(shiftId, cancellationToken);
        if (hasAssignments.Any())
        {
            return Result.Failure(Error.Failure($"Cannot delete shift '{shift.Name}' because {hasAssignments.Count} employee assignment(s) are linked to it. Please reassign or remove employees first."));
        }

        _shiftRepository.Delete(shift);
        await _shiftRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<IReadOnlyList<ShiftAssignmentDto>> GetAssignmentsAsync(
        Guid? shiftId = null,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ShiftAssignment> assignments;

        if (employeeId.HasValue)
        {
            assignments = await _assignmentRepository.GetAssignmentsForEmployeeAsync(employeeId.Value, cancellationToken);
        }
        else if (shiftId.HasValue)
        {
            assignments = await _assignmentRepository.GetAssignmentsForShiftAsync(shiftId.Value, cancellationToken);
        }
        else
        {
            var shifts = await _shiftRepository.GetAllAsync(cancellationToken: cancellationToken);
            assignments = shifts.SelectMany(s => s.Assignments)
                .OrderByDescending(a => a.EffectiveFrom)
                .ToList();
        }

        var result = new List<ShiftAssignmentDto>();
        var employeeCache = new Dictionary<Guid, EmployeeBasicDto?>();

        foreach (var a in assignments)
        {
            if (!employeeCache.TryGetValue(a.EmployeeId, out var emp))
            {
                emp = await _employeeLookup.GetBasicInfoAsync(a.EmployeeId, cancellationToken);
                employeeCache[a.EmployeeId] = emp;
            }

            var empCode = emp?.EmployeeCode ?? "N/A";
            var empName = emp?.FullName ?? "Unknown Employee";

            result.Add(new ShiftAssignmentDto(
                a.ShiftAssignmentId,
                a.EmployeeId,
                empCode,
                empName,
                a.ShiftId,
                a.Shift?.Name ?? "N/A",
                a.EffectiveFrom,
                a.EffectiveTo,
                a.EffectiveTo == null,
                a.CreatedAtUtc
            ));
        }

        return result;
    }

    public async Task<Result<Guid>> AssignShiftAsync(AssignShiftCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _assignValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<Guid>.Failure(Error.Validation(validation.Errors[0].ErrorMessage));
        }

        var employeeExists = await _employeeLookup.ExistsAsync(command.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return Result<Guid>.Failure(Error.NotFound("Employee not found."));
        }

        var shift = await _shiftRepository.GetByIdAsync(command.ShiftId, cancellationToken);
        if (shift == null)
        {
            return Result<Guid>.Failure(Error.NotFound("Shift not found."));
        }

        if (shift.Status != ShiftStatus.Active)
        {
            return Result<Guid>.Failure(Error.Failure("Cannot assign employee to an inactive shift."));
        }

        var overlaps = await _assignmentRepository.HasOverlappingAssignmentAsync(
            command.EmployeeId, command.EffectiveFrom, command.EffectiveTo, cancellationToken: cancellationToken);

        if (overlaps)
        {
            return Result<Guid>.Failure(Error.Conflict("Employee already has an active or overlapping shift assignment during this date range."));
        }

        var assignment = new ShiftAssignment
        {
            ShiftAssignmentId = Guid.NewGuid(),
            EmployeeId = command.EmployeeId,
            ShiftId = command.ShiftId,
            EffectiveFrom = command.EffectiveFrom,
            EffectiveTo = command.EffectiveTo,
            AssignedByUserId = _currentUser.UserId,
            CreatedAtUtc = _clock.UtcNow
        };

        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(assignment.ShiftAssignmentId);
    }

    public async Task<Result<Guid>> TransferShiftAsync(TransferShiftCommand command, CancellationToken cancellationToken = default)
    {
        var employeeExists = await _employeeLookup.ExistsAsync(command.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return Result<Guid>.Failure(Error.NotFound("Employee not found."));
        }

        var newShift = await _shiftRepository.GetByIdAsync(command.NewShiftId, cancellationToken);
        if (newShift == null)
        {
            return Result<Guid>.Failure(Error.NotFound("New shift not found."));
        }

        if (newShift.Status != ShiftStatus.Active)
        {
            return Result<Guid>.Failure(Error.Failure("Cannot transfer employee to an inactive shift."));
        }

        var currentAssignment = await _assignmentRepository.GetCurrentAssignmentAsync(command.EmployeeId, cancellationToken);
        if (currentAssignment != null)
        {
            if (command.EffectiveFrom <= currentAssignment.EffectiveFrom)
            {
                return Result<Guid>.Failure(Error.Failure($"Transfer effective date ({command.EffectiveFrom:yyyy-MM-dd}) must be after current assignment start date ({currentAssignment.EffectiveFrom:yyyy-MM-dd})."));
            }

            currentAssignment.EffectiveTo = command.EffectiveFrom.AddDays(-1);
        }

        var newAssignment = new ShiftAssignment
        {
            ShiftAssignmentId = Guid.NewGuid(),
            EmployeeId = command.EmployeeId,
            ShiftId = command.NewShiftId,
            EffectiveFrom = command.EffectiveFrom,
            EffectiveTo = null,
            AssignedByUserId = _currentUser.UserId,
            CreatedAtUtc = _clock.UtcNow
        };

        await _assignmentRepository.AddAsync(newAssignment, cancellationToken);
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(newAssignment.ShiftAssignmentId);
    }

    public async Task<Result> EndAssignmentAsync(EndShiftAssignmentCommand command, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(command.ShiftAssignmentId, cancellationToken);
        if (assignment == null)
        {
            return Result.Failure(Error.NotFound("Shift assignment not found."));
        }

        if (command.EffectiveTo < assignment.EffectiveFrom)
        {
            return Result.Failure(Error.Failure($"End date ({command.EffectiveTo:yyyy-MM-dd}) cannot be earlier than start date ({assignment.EffectiveFrom:yyyy-MM-dd})."));
        }

        assignment.EffectiveTo = command.EffectiveTo;
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<ShiftAssignmentDto?> GetAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var a = await _assignmentRepository.GetByIdAsync(assignmentId, cancellationToken);
        if (a == null) return null;

        var emp = await _employeeLookup.GetBasicInfoAsync(a.EmployeeId, cancellationToken);
        var today = DateOnly.FromDateTime(_clock.UtcNow);

        return new ShiftAssignmentDto(
            a.ShiftAssignmentId,
            a.EmployeeId,
            emp != null ? $"{emp.FirstName} {emp.LastName}".Trim() : "Unknown Employee",
            emp?.EmployeeCode ?? "",
            a.ShiftId,
            a.Shift?.Name ?? "",
            a.EffectiveFrom,
            a.EffectiveTo,
            a.IsActive(today),
            a.CreatedAtUtc
        );
    }

    public async Task<Result> UpdateAssignmentAsync(Guid assignmentId, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId, cancellationToken);
        if (assignment == null)
        {
            return Result.Failure(Error.NotFound("Shift assignment not found."));
        }

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            return Result.Failure(Error.Validation("Effective To date cannot be earlier than Effective From date."));
        }

        var overlaps = await _assignmentRepository.HasOverlappingAssignmentAsync(
            assignment.EmployeeId,
            effectiveFrom,
            effectiveTo,
            excludeAssignmentId: assignmentId,
            cancellationToken: cancellationToken);

        if (overlaps)
        {
            return Result.Failure(Error.Conflict("The updated date range overlaps with another shift assignment for this employee."));
        }

        assignment.EffectiveFrom = effectiveFrom;
        assignment.EffectiveTo = effectiveTo;
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId, cancellationToken);
        if (assignment == null)
        {
            return Result.Failure(Error.NotFound("Shift assignment not found."));
        }

        _assignmentRepository.Delete(assignment);
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
