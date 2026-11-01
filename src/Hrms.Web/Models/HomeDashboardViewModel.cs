using Hrms.Reports.Application.Models;

namespace Hrms.Web.Models;

public class HomeDashboardViewModel
{
    public bool IsManagementDashboard { get; set; }
    public DashboardSummaryDto? ManagementDashboard { get; set; }
    public EmployeeDashboardDto? EmployeeDashboard { get; set; }
    public bool CanSwitchDashboard => ManagementDashboard != null && EmployeeDashboard != null;
}
