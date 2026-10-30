using Hrms.Assets.Application.Models;
using Hrms.Assets.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Assets.Application.Services;

public interface IAssetService
{
    Task<Result<Guid>> CreateAssetAsync(CreateAssetCommand command, CancellationToken ct = default);
    Task<Result> UpdateAssetAsync(UpdateAssetCommand command, CancellationToken ct = default);
    Task<Result<Guid>> AssignAssetAsync(AssignAssetCommand command, CancellationToken ct = default);
    Task<Result> ReturnAssetAsync(ReturnAssetCommand command, CancellationToken ct = default);
    Task<PagedResult<AssetListItemDto>> GetAssetsAsync(string? search, AssetCategory? category, AssetStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<AssetDetailsDto?> GetAssetDetailsAsync(Guid assetId, CancellationToken ct = default);
    Task<IReadOnlyList<AssetAssignmentHistoryDto>> GetAssetHistoryAsync(Guid assetId, CancellationToken ct = default);
    Task<IReadOnlyList<MyAssetDto>> GetMyAssetsAsync(Guid employeeId, CancellationToken ct = default);
}
