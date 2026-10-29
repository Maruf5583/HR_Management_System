namespace Hrms.Calendar.Domain.Entities;

public class CompanyWorkingDay
{
    public Guid CompanyWorkingDayId { get; set; }

    /// <summary>
    /// 0 = Sunday, 1 = Monday, 2 = Tuesday, 3 = Wednesday, 4 = Thursday, 5 = Friday, 6 = Saturday
    /// </summary>
    public byte DayOfWeek { get; set; }

    /// <summary>
    /// 1 = working day, 0 = weekly off
    /// </summary>
    public bool IsWorkingDay { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public DayOfWeek SystemDayOfWeek => (DayOfWeek)DayOfWeek;
    public string DayName => SystemDayOfWeek.ToString();
}
