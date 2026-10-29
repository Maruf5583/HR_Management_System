using System.Text.Json;
using Hrms.Audit.Domain.Entities;
using Hrms.Audit.Infrastructure.Common;
using Hrms.Calendar.Domain.Entities;
using Hrms.Employees.Domain.Entities;
using Hrms.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Hrms.Persistence.Interceptors;

public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public AuditLogInterceptor(IClock clock, ICurrentUser currentUser)
    {
        _clock = clock;
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CreateAuditLogs(DbContext? context)
    {
        if (context == null) return;

        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;
        var ipAddress = _currentUser.IpAddress;

        var entriesToAudit = context.ChangeTracker.Entries()
            .Where(e => IsAuditableDomainEntity(e.Entity) &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        if (entriesToAudit.Count == 0) return;

        var existingNewLogs = context.ChangeTracker.Entries<AuditLog>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => $"{e.Entity.EntityName}_{e.Entity.EntityId}_{e.Entity.Action}")
            .ToHashSet();

        var auditEntries = new List<AuditLog>();

        foreach (var entry in entriesToAudit)
        {
            var entityName = GetEntityName(entry.Entity);
            var entityId = GetEntityId(entry.Entity);

            if (entry.State == EntityState.Added)
            {
                var logKey = $"{entityName}_{entityId}_CREATE";
                if (existingNewLogs.Contains(logKey)) continue;
                existingNewLogs.Add(logKey);

                var newValues = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties)
                {
                    if (IsIgnoredProperty(prop.Metadata.Name)) continue;
                    newValues[prop.Metadata.Name] = FormatValue(prop.CurrentValue);
                }

                var newJson = AuditDataSanitizer.SanitizeJson(JsonSerializer.Serialize(newValues));

                auditEntries.Add(new AuditLog
                {
                    UserId = userId,
                    Action = "CREATE",
                    EntityName = entityName,
                    EntityId = entityId,
                    OldValues = null,
                    NewValues = newJson,
                    IpAddress = ipAddress,
                    CreatedAtUtc = now
                });
            }
            else if (entry.State == EntityState.Modified)
            {
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();
                var statusOnlyChanged = false;

                foreach (var prop in entry.Properties)
                {
                    if (IsIgnoredProperty(prop.Metadata.Name)) continue;

                    var origVal = prop.OriginalValue;
                    var currVal = prop.CurrentValue;

                    if (!ValuesEqual(origVal, currVal))
                    {
                        oldValues[prop.Metadata.Name] = FormatValue(origVal);
                        newValues[prop.Metadata.Name] = FormatValue(currVal);
                    }
                }

                if (oldValues.Count > 0)
                {
                    if (oldValues.Count == 1 && oldValues.ContainsKey("Status"))
                    {
                        statusOnlyChanged = true;
                    }

                    var action = statusOnlyChanged ? "STATUS_CHANGE" : "UPDATE";
                    var logKey = $"{entityName}_{entityId}_{action}";
                    if (existingNewLogs.Contains(logKey)) continue;
                    existingNewLogs.Add(logKey);

                    var oldJson = AuditDataSanitizer.SanitizeJson(JsonSerializer.Serialize(oldValues));
                    var newJson = AuditDataSanitizer.SanitizeJson(JsonSerializer.Serialize(newValues));

                    auditEntries.Add(new AuditLog
                    {
                        UserId = userId,
                        Action = action,
                        EntityName = entityName,
                        EntityId = entityId,
                        OldValues = oldJson,
                        NewValues = newJson,
                        IpAddress = ipAddress,
                        CreatedAtUtc = now
                    });
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                var logKey = $"{entityName}_{entityId}_DELETE";
                if (existingNewLogs.Contains(logKey)) continue;
                existingNewLogs.Add(logKey);

                var oldValues = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties)
                {
                    if (IsIgnoredProperty(prop.Metadata.Name)) continue;
                    oldValues[prop.Metadata.Name] = FormatValue(prop.OriginalValue);
                }

                var oldJson = AuditDataSanitizer.SanitizeJson(JsonSerializer.Serialize(oldValues));

                auditEntries.Add(new AuditLog
                {
                    UserId = userId,
                    Action = "DELETE",
                    EntityName = entityName,
                    EntityId = entityId,
                    OldValues = oldJson,
                    NewValues = null,
                    IpAddress = ipAddress,
                    CreatedAtUtc = now
                });
            }
        }

        if (auditEntries.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }
    }

    private static bool IsAuditableDomainEntity(object entity)
    {
        return entity is Employee or Department or Designation or Grade
            or Shift or ShiftVersion or ShiftAssignment or Holiday or CompanyWorkingDay;
    }

    private static string GetEntityName(object entity) => entity switch
    {
        Department => "Department",
        Designation => "Designation",
        Grade => "Grade",
        Employee => "Employee",
        Shift => "Shift",
        ShiftVersion => "ShiftVersion",
        ShiftAssignment => "ShiftAssignment",
        Holiday => "Holiday",
        CompanyWorkingDay => "CompanyWorkingDay",
        _ => entity.GetType().Name
    };

    private static string? GetEntityId(object entity) => entity switch
    {
        Department d => d.DepartmentId.ToString(),
        Designation des => des.DesignationId.ToString(),
        Grade g => g.GradeId.ToString(),
        Employee e => e.EmployeeId.ToString(),
        Shift s => s.ShiftId.ToString(),
        ShiftVersion sv => sv.ShiftVersionId.ToString(),
        ShiftAssignment sa => sa.ShiftAssignmentId.ToString(),
        Holiday h => h.HolidayId.ToString(),
        CompanyWorkingDay cwd => cwd.CompanyWorkingDayId.ToString(),
        _ => null
    };

    private static bool IsIgnoredProperty(string propName)
    {
        return propName is "RowVersion"
            or "CreatedAtUtc"
            or "CreatedByUserId"
            or "UpdatedAtUtc"
            or "UpdatedByUserId";
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (Equals(a, b)) return true;
        return string.Equals(a.ToString(), b.ToString(), StringComparison.Ordinal);
    }

    private static object? FormatValue(object? val)
    {
        if (val == null) return null;
        if (val is DateOnly d) return d.ToString("yyyy-MM-dd");
        if (val is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");
        if (val is Enum e) return e.ToString();
        if (val is Guid g) return g.ToString();
        return val;
    }
}
