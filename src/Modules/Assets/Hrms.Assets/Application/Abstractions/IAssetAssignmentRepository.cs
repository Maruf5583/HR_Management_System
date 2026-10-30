using Hrms.Assets.Domain.Entities;

namespace Hrms.Assets.Application.Abstractions;

public interface IAssetAssignmentRepository
{
    Task<AssetAssignment?> GetActiveAssignmentByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<AssetAssignment?> GetByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task AddAsync(AssetAssignment assignment, CancellationToken cancellationToken = default);
}
