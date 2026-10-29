using Hrms.Calendar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Hrms.Calendar.Application.Abstractions;

public interface ICalendarDbContext
{
    DbSet<CompanyWorkingDay> CompanyWorkingDays { get; }
    DbSet<Shift> Shifts { get; }
    DbSet<ShiftVersion> ShiftVersions { get; }
    DbSet<ShiftAssignment> ShiftAssignments { get; }
    DbSet<Holiday> Holidays { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
