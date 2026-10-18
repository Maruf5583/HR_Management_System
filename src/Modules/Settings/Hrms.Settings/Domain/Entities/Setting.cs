namespace Hrms.Settings.Domain.Entities;

public class Setting
{
    public Guid SettingId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string ValueText { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
