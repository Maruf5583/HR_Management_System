using Hrms.Assets.Domain.Enums;

namespace Hrms.Assets.Domain.Entities;

public class AssetRequest
{
    public Guid RequestId { get; set; }
    public Guid AssetId { get; set; }
    public Guid EmployeeId { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AssetRequestStatus Status { get; set; } = AssetRequestStatus.Pending;
    public string? AdminNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }

    // Navigation
    public Asset? Asset { get; set; }

    public static AssetRequest Create(
        Guid assetId,
        Guid employeeId,
        string issueType,
        string description,
        DateTime createdAtUtc)
    {
        return new AssetRequest
        {
            RequestId = Guid.NewGuid(),
            AssetId = assetId,
            EmployeeId = employeeId,
            IssueType = issueType.Trim(),
            Description = description.Trim(),
            Status = AssetRequestStatus.Pending,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void Resolve(AssetRequestStatus newStatus, string? adminNotes, Guid? resolvedByUserId, DateTime resolvedAtUtc)
    {
        Status = newStatus;
        AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        ResolvedByUserId = resolvedByUserId;
        ResolvedAtUtc = resolvedAtUtc;
    }
}
