using Hrms.Settings.Application.Models;
using Hrms.Shared.Primitives;

namespace Hrms.Settings.Application.Services;

public interface ISettingsService
{
    Task<IReadOnlyList<SettingDto>> GetAllSettingsAsync(CancellationToken cancellationToken = default);
    Task<Result> UpdateSettingsAsync(UpdateSettingsCommand command, CancellationToken cancellationToken = default);
}
