using FluentValidation;
using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Application.Models;
using Hrms.Employees.Application.Services;
using Hrms.Employees.Domain.Entities;
using Hrms.Employees.Domain.Enums;
using Hrms.Employees.Domain.Services;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Employees.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private const string DataProtectionPurpose = "Hrms.Employees.Nid";

    private readonly IEmployeeRepository _repository;
    private readonly IDepartmentRepository _deptRepository;
    private readonly IDesignationRepository _desigRepository;
    private readonly IGradeRepository _gradeRepository;
    private readonly IEmployeesDbContext _context;
    private readonly IFileStore _fileStore;
    private readonly IDataProtector _protector;
    private readonly IValidator<CreateEmployeeModel> _createValidator;
    private readonly IValidator<UpdateEmployeeModel> _updateValidator;
    private readonly IValidator<ChangeEmployeeStatusModel> _statusValidator;

    public EmployeeService(
        IEmployeeRepository repository,
        IDepartmentRepository deptRepository,
        IDesignationRepository desigRepository,
        IGradeRepository gradeRepository,
        IEmployeesDbContext context,
        IFileStore fileStore,
        IDataProtectionProvider dataProtectionProvider,
        IValidator<CreateEmployeeModel> createValidator,
        IValidator<UpdateEmployeeModel> updateValidator,
        IValidator<ChangeEmployeeStatusModel> statusValidator)
    {
        _repository = repository;
        _deptRepository = deptRepository;
        _desigRepository = desigRepository;
        _gradeRepository = gradeRepository;
        _context = context;
        _fileStore = fileStore;
        _protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    public async Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(EmployeeFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Designation)
                .ThenInclude(d => d!.Department)
            .Include(e => e.Grade)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(e =>
                e.EmployeeCode.ToLower().Contains(search) ||
                e.FirstName.ToLower().Contains(search) ||
                e.LastName.ToLower().Contains(search) ||
                (e.Email != null && e.Email.ToLower().Contains(search)) ||
                (e.Phone != null && e.Phone.ToLower().Contains(search)));
        }

        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value != Guid.Empty)
        {
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.DesignationId.HasValue && filter.DesignationId.Value != Guid.Empty)
        {
            query = query.Where(e => e.DesignationId == filter.DesignationId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(e => e.Status == filter.Status.Value);
        }

        if (filter.EmploymentType.HasValue)
        {
            query = query.Where(e => e.EmploymentType == filter.EmploymentType.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        var items = await query
            .OrderBy(e => e.EmployeeCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeListItemDto(
                e.EmployeeId,
                e.EmployeeCode,
                e.FirstName,
                e.LastName,
                (e.FirstName + " " + e.LastName).Trim(),
                e.Email,
                e.Phone,
                e.Designation != null && e.Designation.Department != null ? e.Designation.Department.Name : string.Empty,
                e.Designation != null ? e.Designation.Title : string.Empty,
                e.Grade != null ? e.Grade.Code : string.Empty,
                e.EmploymentType,
                e.Status,
                e.JoinDate,
                e.PhotoKey
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeListItemDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<EmployeeDetailsDto?> GetEmployeeDetailsAsync(Guid id, bool canViewNid, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Designation)
                .ThenInclude(d => d!.Department)
            .Include(e => e.Grade)
            .FirstOrDefaultAsync(e => e.EmployeeId == id, cancellationToken);

        if (employee == null)
        {
            return null;
        }

        string? decryptedNid = null;
        if (canViewNid && !string.IsNullOrWhiteSpace(employee.NidNumberEncrypted))
        {
            try
            {
                decryptedNid = _protector.Unprotect(employee.NidNumberEncrypted);
            }
            catch
            {
                decryptedNid = "[Unable to decrypt NID]";
            }
        }

        return new EmployeeDetailsDto(
            employee.EmployeeId,
            employee.EmployeeCode,
            employee.FirstName,
            employee.LastName,
            employee.FullName,
            employee.Email,
            employee.Phone,
            employee.DateOfBirth,
            employee.Gender,
            employee.Address,
            decryptedNid,
            employee.PhotoKey,
            employee.JoinDate,
            employee.EmploymentType,
            employee.DepartmentId,
            employee.Designation != null && employee.Designation.Department != null ? employee.Designation.Department.Name : string.Empty,
            employee.DesignationId,
            employee.Designation != null ? employee.Designation.Title : string.Empty,
            employee.GradeId,
            employee.Grade != null ? employee.Grade.Level : string.Empty,
            employee.Grade != null ? employee.Grade.Code : string.Empty,
            employee.Grade != null ? employee.Grade.Salary : 0m,
            employee.EmergencyContactName,
            employee.EmergencyContactPhone,
            employee.Status,
            employee.ExitDate,
            employee.RowVersion,
            employee.CreatedAtUtc,
            employee.UpdatedAtUtc
        );
    }

    public async Task<EmployeeDetailsDto?> GetMyProfileAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return null;
        }

        return await GetEmployeeDetailsAsync(employeeId, canViewNid: false, cancellationToken);
    }

    public async Task<Result<Guid>> CreateEmployeeAsync(CreateEmployeeModel model, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(model, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<Guid>.Failure(new Error("Validation.Error", validation.Errors[0].ErrorMessage));
        }

        // Validate hierarchy and active status
        var dept = await _deptRepository.GetByIdAsync(model.DepartmentId, cancellationToken);
        var desig = await _desigRepository.GetByIdAsync(model.DesignationId, cancellationToken);
        var grade = await _gradeRepository.GetByIdAsync(model.GradeId, cancellationToken);

        var hierarchyCheck = EmployeeRuleValidator.ValidateHierarchy(dept, desig, grade);
        if (!hierarchyCheck.IsSuccess)
        {
            return Result<Guid>.Failure(hierarchyCheck.Error);
        }

        // Email uniqueness
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            if (await _repository.ExistsByEmailAsync(model.Email.Trim(), null, cancellationToken))
            {
                return Result<Guid>.Failure(new Error("Employee.DuplicateEmail", $"An employee with email '{model.Email.Trim()}' already exists."));
            }
        }

        // Encrypt NID if present
        string? encryptedNid = null;
        if (!string.IsNullOrWhiteSpace(model.NidNumber))
        {
            encryptedNid = _protector.Protect(model.NidNumber.Trim());
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var nextCode = await _repository.GetNextEmployeeCodeAsync(cancellationToken);

            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = nextCode,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
                DateOfBirth = model.DateOfBirth,
                Gender = string.IsNullOrWhiteSpace(model.Gender) ? null : model.Gender.Trim(),
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
                NidNumberEncrypted = encryptedNid,
                JoinDate = model.JoinDate,
                EmploymentType = model.EmploymentType,
                DepartmentId = model.DepartmentId,
                DesignationId = model.DesignationId,
                GradeId = model.GradeId,
                EmergencyContactName = string.IsNullOrWhiteSpace(model.EmergencyContactName) ? null : model.EmergencyContactName.Trim(),
                EmergencyContactPhone = string.IsNullOrWhiteSpace(model.EmergencyContactPhone) ? null : model.EmergencyContactPhone.Trim(),
                Status = EmployeeStatus.Active
            };

            await _repository.AddAsync(employee, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<Guid>.Success(employee.EmployeeId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<Guid>.Failure(new Error("Employee.CreateFailed", ex.Message));
        }
    }

    public async Task<Result> UpdateEmployeeAsync(UpdateEmployeeModel model, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(model, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(new Error("Validation.Error", validation.Errors[0].ErrorMessage));
        }

        var employee = await _repository.GetByIdAsync(model.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Result.Failure(new Error("Employee.NotFound", "Employee not found."));
        }

        // Validate hierarchy and active status
        var dept = await _deptRepository.GetByIdAsync(model.DepartmentId, cancellationToken);
        var desig = await _desigRepository.GetByIdAsync(model.DesignationId, cancellationToken);
        var grade = await _gradeRepository.GetByIdAsync(model.GradeId, cancellationToken);

        var hierarchyCheck = EmployeeRuleValidator.ValidateHierarchy(dept, desig, grade);
        if (!hierarchyCheck.IsSuccess)
        {
            return Result.Failure(hierarchyCheck.Error);
        }

        // Email uniqueness
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            if (await _repository.ExistsByEmailAsync(model.Email.Trim(), model.EmployeeId, cancellationToken))
            {
                return Result.Failure(new Error("Employee.DuplicateEmail", $"An employee with email '{model.Email.Trim()}' already exists."));
            }
        }

        // Concurrency check using RowVersion
        _context.Employees.Entry(employee).Property(e => e.RowVersion).OriginalValue = model.RowVersion;

        employee.FirstName = model.FirstName.Trim();
        employee.LastName = model.LastName.Trim();
        employee.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        employee.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        employee.DateOfBirth = model.DateOfBirth;
        employee.Gender = string.IsNullOrWhiteSpace(model.Gender) ? null : model.Gender.Trim();
        employee.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
        employee.JoinDate = model.JoinDate;
        employee.EmploymentType = model.EmploymentType;
        employee.DepartmentId = model.DepartmentId;
        employee.DesignationId = model.DesignationId;
        employee.GradeId = model.GradeId;
        employee.EmergencyContactName = string.IsNullOrWhiteSpace(model.EmergencyContactName) ? null : model.EmergencyContactName.Trim();
        employee.EmergencyContactPhone = string.IsNullOrWhiteSpace(model.EmergencyContactPhone) ? null : model.EmergencyContactPhone.Trim();

        // Update NID only if provided (can be edited only by authorized caller)
        if (model.NidNumber != null)
        {
            employee.NidNumberEncrypted = string.IsNullOrWhiteSpace(model.NidNumber)
                ? null
                : _protector.Protect(model.NidNumber.Trim());
        }

        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(new Error("Concurrency.Conflict", "Someone else modified this employee record while you were editing it. Please refresh and try again."));
        }
    }

    public async Task<Result> ChangeStatusAsync(ChangeEmployeeStatusModel model, CancellationToken cancellationToken = default)
    {
        var validation = await _statusValidator.ValidateAsync(model, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(new Error("Validation.Error", validation.Errors[0].ErrorMessage));
        }

        var employee = await _repository.GetByIdAsync(model.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Result.Failure(new Error("Employee.NotFound", "Employee not found."));
        }

        var exitDateCheck = EmployeeRuleValidator.ValidateExitDate(model.Status, employee.JoinDate, model.ExitDate);
        if (!exitDateCheck.IsSuccess)
        {
            return Result.Failure(exitDateCheck.Error);
        }

        // Concurrency check
        _context.Employees.Entry(employee).Property(e => e.RowVersion).OriginalValue = model.RowVersion;

        employee.Status = model.Status;
        employee.ExitDate = model.Status is EmployeeStatus.Resigned or EmployeeStatus.Terminated
            ? model.ExitDate
            : null;

        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(new Error("Concurrency.Conflict", "Someone else modified this employee record while you were editing it. Please refresh and try again."));
        }
    }

    public async Task<Result<string>> UploadPhotoAsync(UploadPhotoModel model, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(model.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Result<string>.Failure(new Error("Employee.NotFound", "Employee not found."));
        }

        var photoCheck = EmployeeRuleValidator.ValidatePhoto(model.FileName, model.Content.Length, model.Content);
        if (!photoCheck.IsSuccess)
        {
            return Result<string>.Failure(photoCheck.Error);
        }

        var extension = Path.GetExtension(model.FileName).ToLowerInvariant();
        var folder = $"employees/{employee.EmployeeId}";
        var fileName = $"{Guid.NewGuid():N}{extension}";

        using var memoryStream = new MemoryStream(model.Content);
        var savedKey = await _fileStore.SaveAsync(memoryStream, folder, fileName, cancellationToken);

        // Delete old photo if it exists
        if (!string.IsNullOrWhiteSpace(employee.PhotoKey))
        {
            try
            {
                await _fileStore.DeleteAsync(employee.PhotoKey, cancellationToken);
            }
            catch
            {
                // Non-critical if old file cleanup fails
            }
        }

        employee.PhotoKey = savedKey;
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(savedKey);
    }

    public async Task<Result> RemovePhotoAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(employeeId, cancellationToken);
        if (employee == null)
        {
            return Result.Failure(new Error("Employee.NotFound", "Employee not found."));
        }

        if (!string.IsNullOrWhiteSpace(employee.PhotoKey))
        {
            try
            {
                await _fileStore.DeleteAsync(employee.PhotoKey, cancellationToken);
            }
            catch
            {
                // Non-critical if physical file was already missing
            }

            employee.PhotoKey = null;
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetActiveDepartmentsDropdownAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .Where(d => d.Status == EntityStatus.Active)
            .OrderBy(d => d.Name)
            .Select(d => new LookupItemDto(d.DepartmentId, d.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetActiveDesignationsDropdownAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Designations
            .AsNoTracking()
            .Where(d => d.DepartmentId == departmentId && d.Status == EntityStatus.Active)
            .OrderBy(d => d.Title)
            .Select(d => new LookupItemDto(d.DesignationId, d.Title))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetActiveGradesDropdownAsync(Guid designationId, CancellationToken cancellationToken = default)
    {
        return await _context.Grades
            .AsNoTracking()
            .Where(g => g.DesignationId == designationId && g.Status == EntityStatus.Active)
            .OrderBy(g => g.Level)
            .Select(g => new LookupItemDto(g.GradeId, $"{g.Level} ({g.Code}) - {g.Salary:C}"))
            .ToListAsync(cancellationToken);
    }
}
