using Hrms.Assets.Domain.Enums;

namespace Hrms.Assets.Application.Models;

public record CreateAssetCommand(
    string AssetTag,
    string Name,
    AssetCategory Category,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    AssetCondition Condition,
    string? Notes);

public record UpdateAssetCommand(
    Guid AssetId,
    string Name,
    AssetCategory Category,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    AssetCondition Condition,
    AssetStatus Status,
    string? Notes,
    byte[] RowVersion);

public record AssignAssetCommand(
    Guid AssetId,
    Guid EmployeeId,
    DateTime AssignedAtUtc,
    string? Note);

public record ReturnAssetCommand(
    Guid AssetId,
    DateTime ReturnedAtUtc,
    AssetCondition ReturnedCondition,
    AssetStatus TargetStatus,
    string? Note);

public class AssetListItemDto
{
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AssetCategory Category { get; set; }
    public string? SerialNumber { get; set; }
    public AssetCondition Condition { get; set; }
    public AssetStatus Status { get; set; }
    public Guid? CurrentEmployeeId { get; set; }
    public string? CurrentEmployeeName { get; set; }
    public string? CurrentEmployeeCode { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
}

public class AssetDetailsDto
{
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AssetCategory Category { get; set; }
    public string? SerialNumber { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public AssetCondition Condition { get; set; }
    public AssetStatus Status { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }

    // Current Assignment if assigned
    public Guid? CurrentAssignmentId { get; set; }
    public Guid? CurrentEmployeeId { get; set; }
    public string? CurrentEmployeeName { get; set; }
    public string? CurrentEmployeeCode { get; set; }
    public string? CurrentEmployeeDepartment { get; set; }
    public DateTime? CurrentAssignedAtUtc { get; set; }
    public string? CurrentAssignmentNote { get; set; }
}

public class AssetAssignmentHistoryDto
{
    public Guid AssetAssignmentId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
    public string? Note { get; set; }
    public bool IsCurrentlyHeld => ReturnedAtUtc == null;
}

public class MyAssetDto
{
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AssetCategory Category { get; set; }
    public string? SerialNumber { get; set; }
    public AssetCondition Condition { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public string? Note { get; set; }
}
