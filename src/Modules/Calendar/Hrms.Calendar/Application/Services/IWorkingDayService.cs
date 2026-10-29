using Hrms.Calendar.Application.Models;
using Hrms.Shared.Primitives;

namespace Hrms.Calendar.Application.Services;

public interface IWorkingDayService
{
    Task<IReadOnlyList<CompanyWorkingDayDto>> GetWorkingDaysAsync(CancellationToken cancellationToken = default);
    Task<Result> UpdateWorkingDaysAsync(UpdateWorkingDaysCommand command, CancellationToken cancellationToken = default);
}
