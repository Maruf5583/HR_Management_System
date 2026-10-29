using Hrms.Calendar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Calendar;

public class CompanyWorkingDayConfiguration : IEntityTypeConfiguration<CompanyWorkingDay>
{
    public void Configure(EntityTypeBuilder<CompanyWorkingDay> builder)
    {
        builder.ToTable("CompanyWorkingDays", t =>
        {
            t.HasCheckConstraint("CK_CompanyWorkingDays_Day", "[DayOfWeek] BETWEEN 0 AND 6");
        });

        builder.HasKey(c => c.CompanyWorkingDayId);

        builder.Property(c => c.CompanyWorkingDayId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(c => c.DayOfWeek)
            .IsRequired();

        builder.HasIndex(c => c.DayOfWeek)
            .IsUnique()
            .HasDatabaseName("UQ_CompanyWorkingDays_Day");

        builder.Property(c => c.IsWorkingDay)
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(c => c.UpdatedByUserId);

        builder.Ignore(c => c.SystemDayOfWeek);
        builder.Ignore(c => c.DayName);
    }
}
