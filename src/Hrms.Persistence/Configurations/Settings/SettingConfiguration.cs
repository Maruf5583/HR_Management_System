using Hrms.Settings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Settings;

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");

        builder.HasKey(s => s.SettingId)
            .HasName("PK_Settings");

        builder.Property(s => s.SettingId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(s => s.SettingKey)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(s => s.SettingKey)
            .IsUnique()
            .HasDatabaseName("UQ_Settings_Key");

        builder.Property(s => s.ValueText)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(s => s.UpdatedByUserId)
            .IsRequired(false);
    }
}
