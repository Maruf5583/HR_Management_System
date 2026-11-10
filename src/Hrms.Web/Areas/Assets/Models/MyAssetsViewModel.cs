using Hrms.Assets.Application.Models;

namespace Hrms.Web.Areas.Assets.Models;

public class MyAssetsViewModel
{
    public IReadOnlyList<MyAssetDto> AllocatedAssets { get; set; } = new List<MyAssetDto>();
    public IReadOnlyList<AssetRequestDto> MyRequests { get; set; } = new List<AssetRequestDto>();
}
