using Hrms.Assets.Application.Abstractions;
using Hrms.Assets.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Assets.Infrastructure.Services;

public class AssetLookupService : IAssetLookup
{
    private readonly IAssetsDbContext _dbContext;

    public AssetLookupService(IAssetsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsAssetAssignedAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AssetAssignments
            .AsNoTracking()
            .AnyAsync(a => a.AssetId == assetId && a.ReturnedAtUtc == null, cancellationToken);
    }

    public async Task<IReadOnlyList<AssetLookupDto>> GetEmployeeAssignedAssetsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var assignments = await _dbContext.AssetAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.ReturnedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return Array.Empty<AssetLookupDto>();
        }

        var assetIds = assignments.Select(a => a.AssetId).ToList();
        var assets = await _dbContext.Assets
            .AsNoTracking()
            .Where(a => assetIds.Contains(a.AssetId))
            .ToDictionaryAsync(a => a.AssetId, cancellationToken);

        var list = new List<AssetLookupDto>();
        foreach (var asg in assignments)
        {
            if (assets.TryGetValue(asg.AssetId, out var asset))
            {
                list.Add(new AssetLookupDto
                {
                    AssetId = asset.AssetId,
                    AssetTag = asset.AssetTag,
                    Name = asset.Name,
                    Category = asset.Category.ToString(),
                    SerialNumber = asset.SerialNumber,
                    Condition = asset.Condition.ToString(),
                    AssignedAtUtc = asg.AssignedAtUtc
                });
            }
        }

        return list;
    }
}
