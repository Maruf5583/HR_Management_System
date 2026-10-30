namespace Hrms.Assets.Contracts;

public class AssetLookupDto
{
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateTime AssignedAtUtc { get; set; }
}
