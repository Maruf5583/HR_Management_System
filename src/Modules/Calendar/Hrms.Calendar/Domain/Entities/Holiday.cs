using Hrms.Calendar.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Calendar.Domain.Entities;

public class Holiday : AuditableEntity, IAuditableEntity
{
    public Guid HolidayId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly HolidayDate { get; set; }
    public HolidayType Type { get; set; } = HolidayType.Company;
}
