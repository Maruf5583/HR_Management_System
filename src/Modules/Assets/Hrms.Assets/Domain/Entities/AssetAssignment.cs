namespace Hrms.Assets.Domain.Entities;

public class AssetAssignment
{
    public Guid AssetAssignmentId { get; set; }
    public Guid AssetId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
    public string? Note { get; set; }
    public Guid? AssignedByUserId { get; set; }
    public Guid? ReturnedByUserId { get; set; }

    // Navigation property within module
    public Asset? Asset { get; set; }

    public static AssetAssignment Create(
        Guid assetId,
        Guid employeeId,
        DateTime assignedAtUtc,
        Guid? assignedByUserId,
        string? note)
    {
        return new AssetAssignment
        {
            AssetAssignmentId = Guid.NewGuid(),
            AssetId = assetId,
            EmployeeId = employeeId,
            AssignedAtUtc = assignedAtUtc,
            AssignedByUserId = assignedByUserId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
    }

    public void MarkReturned(DateTime returnedAtUtc, Guid? returnedByUserId, string? note)
    {
        ReturnedAtUtc = returnedAtUtc;
        ReturnedByUserId = returnedByUserId;
        if (!string.IsNullOrWhiteSpace(note))
        {
            Note = string.IsNullOrWhiteSpace(Note) ? note.Trim() : $"{Note} | Return note: {note.Trim()}";
        }
    }
}
