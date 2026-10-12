using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hrms.Persistence;

public class HrmsDbContextFactory : IDesignTimeDbContextFactory<HrmsDbContext>
{
    public HrmsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HrmsDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=HRMS_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new HrmsDbContext(optionsBuilder.Options);
    }
}
