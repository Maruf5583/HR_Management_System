namespace Hrms.Leave.Domain.Entities;

public class LeaveBalance
{
    public Guid LeaveBalanceId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal Entitled { get; set; }
    public decimal Used { get; set; }
    private decimal? _remaining;
    public decimal Remaining
    {
        get => _remaining ?? (Entitled - Used);
        set => _remaining = value;
    }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];

    // Intra-module navigation
    public LeaveType? LeaveType { get; set; }

    public static LeaveBalance Create(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        decimal entitled,
        Guid? createdByUserId = null)
    {
        return new LeaveBalance
        {
            LeaveBalanceId = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            Entitled = entitled,
            Used = 0,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByUserId = createdByUserId
        };
    }
}
