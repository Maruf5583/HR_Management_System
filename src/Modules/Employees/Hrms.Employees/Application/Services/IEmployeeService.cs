using Hrms.Employees.Application.Models;
using Hrms.Shared.Primitives;

namespace Hrms.Employees.Application.Services;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(EmployeeFilterDto filter, CancellationToken cancellationToken = default);
    Task<EmployeeDetailsDto?> GetEmployeeDetailsAsync(Guid id, bool canViewNid, CancellationToken cancellationToken = default);
    Task<EmployeeDetailsDto?> GetMyProfileAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateEmployeeAsync(CreateEmployeeModel model, CancellationToken cancellationToken = default);
    Task<Result> UpdateEmployeeAsync(UpdateEmployeeModel model, CancellationToken cancellationToken = default);
    Task<Result> ChangeStatusAsync(ChangeEmployeeStatusModel model, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadPhotoAsync(UploadPhotoModel model, CancellationToken cancellationToken = default);
    Task<Result> RemovePhotoAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupItemDto>> GetActiveDepartmentsDropdownAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetActiveDesignationsDropdownAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetActiveGradesDropdownAsync(Guid designationId, CancellationToken cancellationToken = default);
}
