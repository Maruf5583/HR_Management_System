using Hrms.Employees.Application.Models;
using Hrms.Shared.Primitives;

namespace Hrms.Employees.Application.Services;

public interface IBankDetailService
{
    Task<EmployeeBankProfileDto> GetEmployeeBankProfileAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<Result<Guid>> SubmitBankDetailAsync(SubmitBankDetailModel model, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<List<BankDetailDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task<Result> ReviewRequestAsync(ReviewBankDetailModel model, Guid reviewerUserId, CancellationToken cancellationToken = default);
}
