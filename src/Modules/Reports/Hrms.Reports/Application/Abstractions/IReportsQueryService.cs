using Hrms.Reports.Application.Models;

namespace Hrms.Reports.Application.Abstractions;

public interface IReportsQueryService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<AttendanceReportSummaryDto> GetAttendanceReportAsync(AttendanceReportFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveBalanceReportItemDto>> GetLeaveBalancesReportAsync(LeaveBalanceReportFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRecordReportItemDto>> GetLeaveRecordsReportAsync(LeaveRecordReportFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeReportItemDto>> GetEmployeeReportAsync(EmployeeReportFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetReportItemDto>> GetAssetReportAsync(AssetReportFilter filter, CancellationToken cancellationToken = default);
}
