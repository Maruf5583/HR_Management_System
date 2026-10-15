using Hrms.Employees.Application.Models;
using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Employees.Application.Services;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateDepartmentAsync(CreateDepartmentModel model, CancellationToken cancellationToken = default);
    Task<Result> UpdateDepartmentAsync(UpdateDepartmentModel model, CancellationToken cancellationToken = default);
    Task<Result> SetDepartmentStatusAsync(Guid id, EntityStatus status, CancellationToken cancellationToken = default);
}

public interface IDesignationService
{
    Task<IReadOnlyList<DesignationDto>> GetDesignationsAsync(Guid? departmentId = null, CancellationToken cancellationToken = default);
    Task<DesignationDto?> GetDesignationByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateDesignationAsync(CreateDesignationModel model, CancellationToken cancellationToken = default);
    Task<Result> UpdateDesignationAsync(UpdateDesignationModel model, CancellationToken cancellationToken = default);
    Task<Result> SetDesignationStatusAsync(Guid id, EntityStatus status, CancellationToken cancellationToken = default);
}

public interface IGradeService
{
    Task<IReadOnlyList<GradeDto>> GetGradesAsync(Guid? designationId = null, CancellationToken cancellationToken = default);
    Task<GradeDto?> GetGradeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateGradeAsync(CreateGradeModel model, CancellationToken cancellationToken = default);
    Task<Result> UpdateGradeAsync(UpdateGradeModel model, CancellationToken cancellationToken = default);
    Task<Result> SetGradeStatusAsync(Guid id, EntityStatus status, CancellationToken cancellationToken = default);
}
