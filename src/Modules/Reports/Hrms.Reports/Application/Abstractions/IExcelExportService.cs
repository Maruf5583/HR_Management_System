using Hrms.Reports.Application.Models;

namespace Hrms.Reports.Application.Abstractions;

public interface IExcelExportService
{
    byte[] ExportAttendanceReport(AttendanceReportSummaryDto data);
    byte[] ExportLeaveBalancesReport(IReadOnlyList<LeaveBalanceReportItemDto> items, int year);
    byte[] ExportLeaveRecordsReport(IReadOnlyList<LeaveRecordReportItemDto> items);
    byte[] ExportEmployeeReport(IReadOnlyList<EmployeeReportItemDto> items);
    byte[] ExportAssetReport(IReadOnlyList<AssetReportItemDto> items);
}
