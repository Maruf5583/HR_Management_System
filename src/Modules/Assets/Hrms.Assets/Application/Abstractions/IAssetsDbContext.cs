using Hrms.Assets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Assets.Application.Abstractions;

public interface IAssetsDbContext
{
    DbSet<Asset> Assets { get; }
    DbSet<AssetAssignment> AssetAssignments { get; }
    DbSet<AssetRequest> AssetRequests { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
