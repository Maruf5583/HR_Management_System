namespace Hrms.Assets.Contracts;

public interface IAssetLookup
{
    Task<bool> IsAssetAssignedAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetLookupDto>> GetEmployeeAssignedAssetsAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
