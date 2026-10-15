namespace Hrms.Employees.Contracts;

public record EmployeeBasicDto(
    Guid EmployeeId,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? Email,
    string Status,
    DateOnly JoinDate,
    DateOnly? ExitDate,
    Guid DepartmentId,
    decimal GradeSalary
)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public record EmployeeLookupDto(
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    string? Email
);

public interface IEmployeeLookup
{
    Task<bool> ExistsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeBasicDto?> GetBasicInfoAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeLookupDto>> GetActiveEmployeesDropdownAsync(CancellationToken cancellationToken = default);
}
