using Hrms.Assets.Domain.Entities;
using Hrms.Assets.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Assets.Domain.Services;

public static class AssetRuleValidator
{
    public static Result ValidateAssignment(
        Asset? asset,
        AssetAssignment? activeAssignment,
        string? employeeStatus)
    {
        if (asset == null)
        {
            return Result.Failure(new Error("Asset.NotFound", "Asset not found."));
        }

        if (asset.Status != AssetStatus.Available)
        {
            return Result.Failure(new Error("Asset.NotAvailable", $"Asset '{asset.AssetTag}' is currently {asset.Status} and cannot be assigned."));
        }

        if (activeAssignment != null)
        {
            return Result.Failure(new Error("Asset.AlreadyAssigned", "Asset already has an active assignment."));
        }

        if (string.IsNullOrWhiteSpace(employeeStatus))
        {
            return Result.Failure(new Error("Employee.NotFound", "Selected employee was not found."));
        }

        if (!string.Equals(employeeStatus, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(new Error("Employee.Inactive", "Assets can only be assigned to active employees."));
        }

        return Result.Success();
    }

    public static Result ValidateReturn(
        Asset? asset,
        AssetAssignment? activeAssignment,
        DateTime returnedAtUtc)
    {
        if (asset == null)
        {
            return Result.Failure(new Error("Asset.NotFound", "Asset not found."));
        }

        if (activeAssignment == null)
        {
            return Result.Failure(new Error("Asset.NoActiveAssignment", "Asset has no active assignment to return."));
        }

        if (returnedAtUtc < activeAssignment.AssignedAtUtc)
        {
            return Result.Failure(new Error("Asset.InvalidReturnDate", "Return date cannot be earlier than the assignment date."));
        }

        return Result.Success();
    }

    public static Result ValidateStatusTransition(
        Asset asset,
        AssetAssignment? activeAssignment,
        AssetStatus newStatus)
    {
        if (activeAssignment != null && (newStatus == AssetStatus.Available || newStatus == AssetStatus.InRepair || newStatus == AssetStatus.Retired))
        {
            return Result.Failure(new Error("Asset.HasActiveAssignment", $"Cannot change status to {newStatus} while an employee holds an active assignment. Please return the asset first."));
        }

        return Result.Success();
    }
}
