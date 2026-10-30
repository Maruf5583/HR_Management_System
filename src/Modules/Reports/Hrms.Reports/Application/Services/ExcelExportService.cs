using ClosedXML.Excel;
using Hrms.Reports.Application.Abstractions;
using Hrms.Reports.Application.Models;

namespace Hrms.Reports.Application.Services;

public class ExcelExportService : IExcelExportService
{
    private static readonly XLColor HeaderBg = XLColor.FromHtml("#1E293B"); // Slate dark
    private static readonly XLColor HeaderFg = XLColor.White;
    private static readonly XLColor ZebraBg = XLColor.FromHtml("#F8FAFC"); // Slate light

    public byte[] ExportAttendanceReport(AttendanceReportSummaryDto data)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Attendance Report");

        // Title and Summary Block
        ws.Cell(1, 1).Value = "Attendance Summary Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        ws.Cell(2, 1).Value = $"Period: {data.FromDate:yyyy-MM-dd} to {data.ToDate:yyyy-MM-dd} | Total Records: {data.TotalRecords}";
        ws.Cell(2, 1).Style.Font.Italic = true;

        ws.Cell(3, 1).Value = $"Present: {data.PresentCount} | Late: {data.LateCount} | Half Day: {data.HalfDayCount} | Absent: {data.AbsentCount} | On Leave: {data.OnLeaveCount} | Holiday: {data.HolidayCount} | Weekend: {data.WeekendCount}";
        ws.Cell(3, 1).Style.Font.Bold = true;

        // Table Header
        int startRow = 5;
        string[] headers =
        [
            "Date", "Employee Code", "Employee Name", "Department", "Designation",
            "Shift", "Check In", "Check Out", "Late (Mins)", "Worked (Mins)", "Status", "Note"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(startRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = HeaderBg;
            cell.Style.Font.FontColor = HeaderFg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Rows
        int r = startRow + 1;
        foreach (var item in data.Items)
        {
            ws.Cell(r, 1).Value = item.WorkDate.ToString("yyyy-MM-dd");
            ws.Cell(r, 2).Value = item.EmployeeCode;
            ws.Cell(r, 3).Value = item.FullName;
            ws.Cell(r, 4).Value = item.DepartmentName;
            ws.Cell(r, 5).Value = item.DesignationTitle;
            ws.Cell(r, 6).Value = item.ShiftName ?? "-";
            ws.Cell(r, 7).Value = item.CheckInTime?.ToString("HH:mm") ?? "-";
            ws.Cell(r, 8).Value = item.CheckOutTime?.ToString("HH:mm") ?? "-";
            ws.Cell(r, 9).Value = item.LateMinutes;
            ws.Cell(r, 10).Value = item.WorkedMinutes;
            ws.Cell(r, 11).Value = item.Status.ToString();
            ws.Cell(r, 12).Value = item.Note ?? "";

            if (r % 2 == 0)
            {
                ws.Range(r, 1, r, headers.Length).Style.Fill.BackgroundColor = ZebraBg;
            }

            r++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportLeaveBalancesReport(IReadOnlyList<LeaveBalanceReportItemDto> items, int year)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Leave Balances");

        ws.Cell(1, 1).Value = $"Employee Leave Balances - Year {year}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        int startRow = 3;
        string[] headers =
        [
            "Employee Code", "Employee Name", "Department", "Leave Type", "Year", "Entitled Days", "Used Days", "Remaining Days"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(startRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = HeaderBg;
            cell.Style.Font.FontColor = HeaderFg;
        }

        int r = startRow + 1;
        foreach (var item in items)
        {
            ws.Cell(r, 1).Value = item.EmployeeCode;
            ws.Cell(r, 2).Value = item.FullName;
            ws.Cell(r, 3).Value = item.DepartmentName;
            ws.Cell(r, 4).Value = item.LeaveTypeName;
            ws.Cell(r, 5).Value = item.Year;
            ws.Cell(r, 6).Value = item.Entitled;
            ws.Cell(r, 7).Value = item.Used;
            ws.Cell(r, 8).Value = item.Remaining;

            if (r % 2 == 0)
            {
                ws.Range(r, 1, r, headers.Length).Style.Fill.BackgroundColor = ZebraBg;
            }

            r++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportLeaveRecordsReport(IReadOnlyList<LeaveRecordReportItemDto> items)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Leave Records");

        ws.Cell(1, 1).Value = "Leave Applications & Records Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        int startRow = 3;
        string[] headers =
        [
            "Employee Code", "Employee Name", "Department", "Leave Type",
            "Start Date", "End Date", "Total Days", "Half Day", "Status", "Reason", "Applied Date (UTC)"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(startRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = HeaderBg;
            cell.Style.Font.FontColor = HeaderFg;
        }

        int r = startRow + 1;
        foreach (var item in items)
        {
            ws.Cell(r, 1).Value = item.EmployeeCode;
            ws.Cell(r, 2).Value = item.FullName;
            ws.Cell(r, 3).Value = item.DepartmentName;
            ws.Cell(r, 4).Value = item.LeaveTypeName;
            ws.Cell(r, 5).Value = item.StartDate.ToString("yyyy-MM-dd");
            ws.Cell(r, 6).Value = item.EndDate.ToString("yyyy-MM-dd");
            ws.Cell(r, 7).Value = item.TotalDays;
            ws.Cell(r, 8).Value = item.IsHalfDay ? "Yes" : "No";
            ws.Cell(r, 9).Value = item.Status.ToString();
            ws.Cell(r, 10).Value = item.Reason ?? "";
            ws.Cell(r, 11).Value = item.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm");

            if (r % 2 == 0)
            {
                ws.Range(r, 1, r, headers.Length).Style.Fill.BackgroundColor = ZebraBg;
            }

            r++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportEmployeeReport(IReadOnlyList<EmployeeReportItemDto> items)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Employee Directory");

        ws.Cell(1, 1).Value = "Company Employee Directory Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        int startRow = 3;
        string[] headers =
        [
            "Employee Code", "Full Name", "Email", "Phone", "Date of Birth", "Gender",
            "Department", "Designation", "Grade", "Basic Salary", "Join Date", "Employment Type", "Status", "Exit Date"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(startRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = HeaderBg;
            cell.Style.Font.FontColor = HeaderFg;
        }

        int r = startRow + 1;
        foreach (var item in items)
        {
            ws.Cell(r, 1).Value = item.EmployeeCode;
            ws.Cell(r, 2).Value = item.FullName;
            ws.Cell(r, 3).Value = item.Email;
            ws.Cell(r, 4).Value = item.Phone;
            ws.Cell(r, 5).Value = item.DateOfBirth?.ToString("yyyy-MM-dd") ?? "-";
            ws.Cell(r, 6).Value = item.Gender ?? "-";
            ws.Cell(r, 7).Value = item.DepartmentName;
            ws.Cell(r, 8).Value = item.DesignationTitle;
            ws.Cell(r, 9).Value = item.GradeCode;
            ws.Cell(r, 10).Value = item.Salary;
            ws.Cell(r, 11).Value = item.JoinDate.ToString("yyyy-MM-dd");
            ws.Cell(r, 12).Value = item.EmploymentType.ToString();
            ws.Cell(r, 13).Value = item.Status.ToString();
            ws.Cell(r, 14).Value = item.ExitDate?.ToString("yyyy-MM-dd") ?? "-";

            if (r % 2 == 0)
            {
                ws.Range(r, 1, r, headers.Length).Style.Fill.BackgroundColor = ZebraBg;
            }

            r++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportAssetReport(IReadOnlyList<AssetReportItemDto> items)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Assets");

        ws.Cell(1, 1).Value = "Company Assets Inventory & Allocation Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        int startRow = 3;
        string[] headers =
        [
            "Asset Tag", "Asset Name", "Category", "Serial Number", "Purchase Date",
            "Condition", "Status", "Assigned Employee Code", "Assigned Employee Name", "Department", "Assigned Date (UTC)"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(startRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = HeaderBg;
            cell.Style.Font.FontColor = HeaderFg;
        }

        int r = startRow + 1;
        foreach (var item in items)
        {
            ws.Cell(r, 1).Value = item.AssetTag;
            ws.Cell(r, 2).Value = item.Name;
            ws.Cell(r, 3).Value = item.Category.ToString();
            ws.Cell(r, 4).Value = item.SerialNumber ?? "-";
            ws.Cell(r, 5).Value = item.PurchaseDate?.ToString("yyyy-MM-dd") ?? "-";
            ws.Cell(r, 6).Value = item.Condition.ToString();
            ws.Cell(r, 7).Value = item.Status.ToString();
            ws.Cell(r, 8).Value = item.AssignedToEmployeeCode ?? "-";
            ws.Cell(r, 9).Value = item.AssignedToFullName ?? "-";
            ws.Cell(r, 10).Value = item.AssignedToDepartment ?? "-";
            ws.Cell(r, 11).Value = item.AssignedAtUtc?.ToString("yyyy-MM-dd HH:mm") ?? "-";

            if (r % 2 == 0)
            {
                ws.Range(r, 1, r, headers.Length).Style.Fill.BackgroundColor = ZebraBg;
            }

            r++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
