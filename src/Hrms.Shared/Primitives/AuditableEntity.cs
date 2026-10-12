namespace Hrms.Shared.Primitives;

public abstract class AuditableEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    Guid? CreatedByUserId { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    Guid? UpdatedByUserId { get; set; }
}
