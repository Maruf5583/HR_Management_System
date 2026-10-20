using System.Text.Json;
using Hrms.Audit.Application.Services;
using Hrms.Identity.Contracts;
using Hrms.Persistence;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Audit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Web.Areas.Audit.Controllers;

[Area("Audit")]
[Authorize(Policy = Permissions.Audit.View)]
public class AuditController : Controller
{
    private readonly IAuditLogQueryService _queryService;
    private readonly IIdentityService _identityService;
    private readonly HrmsDbContext _dbContext;

    public AuditController(
        IAuditLogQueryService queryService,
        IIdentityService identityService,
        HrmsDbContext dbContext)
    {
        _queryService = queryService;
        _identityService = identityService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AuditLogFilterViewModel filter, CancellationToken cancellationToken = default)
    {
        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 20;

        var filterDto = filter.ToFilterDto();
        var pagedLogs = await _queryService.GetPagedAsync(filterDto, cancellationToken);

        var distinctEntities = await _queryService.GetDistinctEntityNamesAsync(cancellationToken);
        var distinctActions = await _queryService.GetDistinctActionsAsync(cancellationToken);

        var userDisplayNames = new Dictionary<Guid, string>();
        var userIds = pagedLogs.Items
            .Where(x => x.UserId.HasValue)
            .Select(x => x.UserId!.Value)
            .Distinct();

        foreach (var userId in userIds)
        {
            var email = await _identityService.GetUserEmailAsync(userId, cancellationToken);
            userDisplayNames[userId] = !string.IsNullOrWhiteSpace(email) ? email : userId.ToString();
        }

        var viewModel = new AuditLogIndexViewModel
        {
            Logs = pagedLogs,
            Filter = filter,
            AvailableEntities = distinctEntities
                .Select(e => new SelectListItem(e, e, e == filter.EntityName))
                .ToList(),
            AvailableActions = distinctActions
                .Select(a => new SelectListItem(a, a, a == filter.Action))
                .ToList(),
            UserDisplayNames = userDisplayNames
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken = default)
    {
        var log = await _queryService.GetByIdAsync(id, cancellationToken);
        if (log == null)
        {
            return NotFound();
        }

        string? userEmail = null;
        if (log.UserId.HasValue)
        {
            userEmail = await _identityService.GetUserEmailAsync(log.UserId.Value, cancellationToken);
        }

        var resolvedNames = new Dictionary<string, string>();
        await TryResolveForeignNamesAsync(log.OldValues, log.NewValues, resolvedNames, cancellationToken);

        if (Request.Headers.XRequestedWith == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Json(new
            {
                auditLogId = log.AuditLogId,
                entityName = log.EntityName,
                entityId = log.EntityId,
                action = log.Action,
                createdAtUtc = log.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
                userId = log.UserId,
                userEmail = userEmail ?? "System / Anonymous",
                ipAddress = DisplayIp(log.IpAddress),
                oldValues = log.OldValues,
                newValues = log.NewValues,
                resolvedNames
            });
        }

        ViewBag.UserEmail = userEmail ?? "System / Anonymous";
        ViewBag.ResolvedNames = resolvedNames;
        return View(log);
    }

    private async Task TryResolveForeignNamesAsync(
        string? oldValuesJson,
        string? newValuesJson,
        Dictionary<string, string> resolvedNames,
        CancellationToken cancellationToken)
    {
        var guidsToResolve = new HashSet<Guid>();
        ExtractGuids(oldValuesJson, guidsToResolve);
        ExtractGuids(newValuesJson, guidsToResolve);

        if (guidsToResolve.Count == 0) return;

        var departments = await _dbContext.Departments
            .AsNoTracking()
            .Where(d => guidsToResolve.Contains(d.DepartmentId))
            .Select(d => new { d.DepartmentId, d.Name })
            .ToListAsync(cancellationToken);

        foreach (var d in departments)
        {
            resolvedNames[d.DepartmentId.ToString()] = d.Name;
        }

        var designations = await _dbContext.Designations
            .AsNoTracking()
            .Where(d => guidsToResolve.Contains(d.DesignationId))
            .Select(d => new { d.DesignationId, d.Title })
            .ToListAsync(cancellationToken);

        foreach (var d in designations)
        {
            resolvedNames[d.DesignationId.ToString()] = d.Title;
        }

        var grades = await _dbContext.Grades
            .AsNoTracking()
            .Where(g => guidsToResolve.Contains(g.GradeId))
            .Select(g => new { g.GradeId, Display = g.Level + " (" + g.Code + ")" })
            .ToListAsync(cancellationToken);

        foreach (var g in grades)
        {
            resolvedNames[g.GradeId.ToString()] = g.Display;
        }

        var employees = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => guidsToResolve.Contains(e.EmployeeId))
            .Select(e => new { e.EmployeeId, FullName = (e.FirstName + " " + e.LastName).Trim() })
            .ToListAsync(cancellationToken);

        foreach (var e in employees)
        {
            resolvedNames[e.EmployeeId.ToString()] = e.FullName;
        }
    }

    private static void ExtractGuids(string? json, HashSet<Guid> guids)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(prop.Value.GetString(), out var g))
                    {
                        guids.Add(g);
                    }
                }
            }
        }
        catch
        {
            // Ignore non-json or malformed
        }
    }

    private static string DisplayIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return "N/A";
        if (ip == "::1" || ip == "0:0:0:0:0:0:0:1") return "127.0.0.1";
        if (ip.StartsWith("::ffff:")) return ip[7..];
        return ip;
    }
}
