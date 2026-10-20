using Hrms.Audit.Contracts.Models;
using Hrms.Shared.Primitives;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Areas.Audit.Models;

public class AuditLogIndexViewModel
{
    public PagedResult<AuditLogDto> Logs { get; set; } = new(new List<AuditLogDto>(), 0, 1, 20);
    public AuditLogFilterViewModel Filter { get; set; } = new();
    public List<SelectListItem> AvailableEntities { get; set; } = new();
    public List<SelectListItem> AvailableActions { get; set; } = new();
    public Dictionary<Guid, string> UserDisplayNames { get; set; } = new();
}

public class AuditLogFilterViewModel
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? EntityName { get; set; }
    public string? Action { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public AuditLogFilterDto ToFilterDto() =>
        new(DateFrom, DateTo, EntityName, Action, null, SearchTerm, PageNumber, PageSize);
}
