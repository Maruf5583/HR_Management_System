using Hrms.Assets.Application.Abstractions;
using Hrms.Assets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Assets.Infrastructure.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly IAssetsDbContext _dbContext;

    public AssetRepository(IAssetsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Asset?> GetByIdAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Assets
            .FirstOrDefaultAsync(a => a.AssetId == assetId, cancellationToken);
    }

    public async Task<Asset?> GetByTagAsync(string assetTag, CancellationToken cancellationToken = default)
    {
        var normalized = assetTag.Trim().ToLower();
        return await _dbContext.Assets
            .FirstOrDefaultAsync(a => a.AssetTag.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> ExistsTagAsync(string assetTag, Guid? excludeAssetId = null, CancellationToken cancellationToken = default)
    {
        var normalized = assetTag.Trim().ToLower();
        var query = _dbContext.Assets.Where(a => a.AssetTag.ToLower() == normalized);
        if (excludeAssetId.HasValue)
        {
            query = query.Where(a => a.AssetId != excludeAssetId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsSerialNumberAsync(string serialNumber, Guid? excludeAssetId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return false;
        }

        var normalized = serialNumber.Trim().ToLower();
        var query = _dbContext.Assets.Where(a => a.SerialNumber != null && a.SerialNumber.ToLower() == normalized);
        if (excludeAssetId.HasValue)
        {
            query = query.Where(a => a.AssetId != excludeAssetId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        await _dbContext.Assets.AddAsync(asset, cancellationToken);
    }

    public void Update(Asset asset)
    {
        _dbContext.Assets.Update(asset);
    }
}
