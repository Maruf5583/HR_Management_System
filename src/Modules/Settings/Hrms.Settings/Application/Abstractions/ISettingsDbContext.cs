using Hrms.Settings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Hrms.Settings.Application.Abstractions;

public interface ISettingsDbContext
{
    DbSet<Setting> Settings { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
