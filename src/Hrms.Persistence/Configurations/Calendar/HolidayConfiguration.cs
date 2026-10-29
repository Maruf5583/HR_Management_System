using Hrms.Calendar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Calendar;

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays", t =>
        {
            t.HasCheckConstraint("CK_Holidays_Type", "[Type] IN ('Government','Company')");
        });

        builder.HasKey(h => h.HolidayId);

        builder.Property(h => h.HolidayId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(h => h.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(h => h.HolidayDate)
            .HasColumnType("date")
            .IsRequired();

        builder.HasIndex(h => h.HolidayDate)
            .IsUnique()
            .HasDatabaseName("UQ_Holidays_Date");

        builder.Property(h => h.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(h => h.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(h => h.CreatedByUserId);

        builder.Property(h => h.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(h => h.UpdatedByUserId);
    }
}
