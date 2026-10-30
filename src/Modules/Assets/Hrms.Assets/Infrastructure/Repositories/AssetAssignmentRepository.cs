using Hrms.Assets.Application.Abstractions;
using Hrms.Assets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Assets.Infrastructure.Repositories;

public class AssetAssignmentRepository : IAssetAssignmentRepository
{
    private readonly IAssetsDbContext _dbContext;

    public AssetAssignmentRepository(IAssetsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AssetAssignment?> GetActiveAssignmentByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AssetAssignments
            .FirstOrDefaultAsync(a => a.AssetId == assetId && a.ReturnedAtUtc == null, cancellationToken);
    }

    public async Task<AssetAssignment?> GetByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AssetAssignments
            .FirstOrDefaultAsync(a => a.AssetAssignmentId == assignmentId, cancellationToken);
    }

    public async Task AddAsync(AssetAssignment assignment, CancellationToken cancellationToken = default)
    {
        await _dbContext.AssetAssignments.AddAsync(assignment, cancellationToken);
    }
}
