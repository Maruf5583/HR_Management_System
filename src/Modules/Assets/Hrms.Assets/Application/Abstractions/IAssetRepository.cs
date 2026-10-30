using Hrms.Assets.Domain.Entities;

namespace Hrms.Assets.Application.Abstractions;

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<Asset?> GetByTagAsync(string assetTag, CancellationToken cancellationToken = default);
    Task<bool> ExistsTagAsync(string assetTag, Guid? excludeAssetId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsSerialNumberAsync(string serialNumber, Guid? excludeAssetId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Asset asset, CancellationToken cancellationToken = default);
    void Update(Asset asset);
}
