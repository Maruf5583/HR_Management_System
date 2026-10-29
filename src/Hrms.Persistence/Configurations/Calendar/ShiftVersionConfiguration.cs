using Hrms.Calendar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Calendar;

public class ShiftVersionConfiguration : IEntityTypeConfiguration<ShiftVersion>
{
    public void Configure(EntityTypeBuilder<ShiftVersion> builder)
    {
        builder.ToTable("ShiftVersions", t =>
        {
            t.HasCheckConstraint("CK_ShiftVersions_Minutes", "[GraceMinutes] >= 0 AND [BreakMinutes] >= 0");
            t.HasCheckConstraint("CK_ShiftVersions_Range", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });

        builder.HasKey(v => v.ShiftVersionId);

        builder.Property(v => v.ShiftVersionId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(v => v.ShiftId)
            .IsRequired();

        builder.Property(v => v.StartTime)
            .HasColumnType("time(0)")
            .IsRequired();

        builder.Property(v => v.EndTime)
            .HasColumnType("time(0)")
            .IsRequired();

        builder.Property(v => v.GraceMinutes)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(v => v.BreakMinutes)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(v => v.EffectiveFrom)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(v => v.EffectiveTo)
            .HasColumnType("date");

        builder.Property(v => v.ChangedByUserId);

        builder.Property(v => v.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.HasIndex(v => new { v.ShiftId, v.EffectiveFrom })
            .IsUnique()
            .HasDatabaseName("UQ_ShiftVersions_Shift_From");

        builder.HasIndex(v => v.ShiftId)
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL")
            .HasDatabaseName("UX_ShiftVersions_Current");

        builder.Ignore(v => v.IsNightShift);
        builder.Ignore(v => v.TotalShiftSpan);
        builder.Ignore(v => v.NetWorkingSpan);
    }
}
