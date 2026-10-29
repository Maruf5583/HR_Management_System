namespace Hrms.Shared.Constants;

public static class Permissions
{
    public static class Users
    {
        public const string Manage = "users.manage";
    }

    public static class Roles
    {
        public const string Manage = "roles.manage";
    }

    public static class Employees
    {
        public const string View = "employees.view";
        public const string Manage = "employees.manage";
        public const string ViewNid = "employees.view_nid";
        public const string ViewOwn = "employees.view_own";
    }

    public static class Attendance
    {
        public const string ViewOwn = "attendance.view_own";
        public const string ViewAll = "attendance.view_all";
        public const string Manage = "attendance.manage";
    }

    public static class Shifts
    {
        public const string Manage = "shifts.manage";
        public const string ViewOwn = "shifts.view_own";
    }

    public static class Holidays
    {
        public const string Manage = "holidays.manage";
        public const string View = "holidays.view";
    }

    public static class Leave
    {
        public const string ViewOwn = "leave.view_own";
        public const string ViewAll = "leave.view_all";
        public const string Manage = "leave.manage";
    }

    public static class Payroll
    {
        public const string ViewOwn = "payroll.view_own";
        public const string ViewAll = "payroll.view_all";
        public const string Generate = "payroll.generate";
        public const string Publish = "payroll.publish";
    }

    public static class Assets
    {
        public const string ViewOwn = "assets.view_own";
        public const string Manage = "assets.manage";
    }

    public static class Reports
    {
        public const string View = "reports.view";
    }

    public static class Settings
    {
        public const string Manage = "settings.manage";
    }

    public static class Audit
    {
        public const string View = "audit.view";
    }

    public static readonly IReadOnlyList<string> All = new[]
    {
        Users.Manage,
        Roles.Manage,
        Employees.View,
        Employees.Manage,
        Employees.ViewNid,
        Employees.ViewOwn,
        Attendance.ViewOwn,
        Attendance.ViewAll,
        Attendance.Manage,
        Shifts.Manage,
        Shifts.ViewOwn,
        Holidays.View,
        Leave.ViewOwn,
        Leave.ViewAll,
        Leave.Manage,
        Payroll.ViewOwn,
        Payroll.ViewAll,
        Payroll.Generate,
        Payroll.Publish,
        Assets.ViewOwn,
        Assets.Manage,
        Reports.View,
        Settings.Manage,
        Audit.View
    };

    public static readonly IReadOnlyList<string> AdminDefault = All;

    public static readonly IReadOnlyList<string> HrDefault = All
        .Except(new[]
        {
            Users.Manage,
            Roles.Manage,
            Settings.Manage,
            Audit.View
        })
        .ToList();

    public static readonly IReadOnlyList<string> EmployeeDefault = new[]
    {
        Employees.ViewOwn,
        Attendance.ViewOwn,
        Shifts.ViewOwn,
        Holidays.View,
        Leave.ViewOwn,
        Payroll.ViewOwn,
        Assets.ViewOwn
    };
}

