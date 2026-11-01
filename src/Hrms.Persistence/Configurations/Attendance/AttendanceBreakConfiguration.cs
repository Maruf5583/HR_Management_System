using Hrms.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Attendance;

public class AttendanceBreakConfiguration : IEntityTypeConfiguration<AttendanceBreak>
{
    public void Configure(EntityTypeBuilder<AttendanceBreak> builder)
    {
        builder.ToTable("AttendanceBreaks");

        builder.HasKey(b => b.AttendanceBreakId)
            .HasName("PK_AttendanceBreaks");

        builder.Property(b => b.AttendanceBreakId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(b => b.AttendanceDayId)
            .IsRequired();

        builder.Property(b => b.BreakStartUtc)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(b => b.BreakEndUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(b => b.Minutes)
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasIndex(b => b.AttendanceDayId)
            .HasDatabaseName("IX_AttendanceBreaks_AttendanceDayId");
    }
}
