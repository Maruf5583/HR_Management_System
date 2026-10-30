using Hrms.Assets.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Assets.Domain.Entities;

public class Asset : AuditableEntity
{
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AssetCategory Category { get; set; }
    public string? SerialNumber { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public AssetCondition Condition { get; set; } = AssetCondition.New;
    public AssetStatus Status { get; set; } = AssetStatus.Available;
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = [];

    // Navigation property within module
    public ICollection<AssetAssignment> Assignments { get; set; } = new List<AssetAssignment>();

    public static Asset Create(
        string assetTag,
        string name,
        AssetCategory category,
        string? serialNumber,
        DateOnly? purchaseDate,
        AssetCondition condition,
        string? notes)
    {
        return new Asset
        {
            AssetId = Guid.NewGuid(),
            AssetTag = assetTag.Trim(),
            Name = name.Trim(),
            Category = category,
            SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim(),
            PurchaseDate = purchaseDate,
            Condition = condition,
            Status = AssetStatus.Available,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }

    public void UpdateDetails(
        string name,
        AssetCategory category,
        string? serialNumber,
        DateOnly? purchaseDate,
        AssetCondition condition,
        AssetStatus status,
        string? notes)
    {
        Name = name.Trim();
        Category = category;
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
        PurchaseDate = purchaseDate;
        Condition = condition;
        Status = status;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void MarkAssigned()
    {
        Status = AssetStatus.Assigned;
    }

    public void MarkReturned(AssetCondition returnedCondition, AssetStatus targetStatus = AssetStatus.Available)
    {
        Condition = returnedCondition;
        Status = targetStatus;
    }
}
