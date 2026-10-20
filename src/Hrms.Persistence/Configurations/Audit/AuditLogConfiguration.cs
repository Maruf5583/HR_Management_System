using Hrms.Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Audit;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", t =>
        {
            t.HasCheckConstraint("CK_AuditLogs_OldJson", "[OldValues] IS NULL OR ISJSON([OldValues]) = 1");
            t.HasCheckConstraint("CK_AuditLogs_NewJson", "[NewValues] IS NULL OR ISJSON([NewValues]) = 1");
        });

        builder.HasKey(a => a.AuditLogId)
            .HasName("PK_AuditLogs");

        builder.Property(a => a.AuditLogId)
            .UseIdentityColumn();

        builder.Property(a => a.UserId)
            .IsRequired(false);

        builder.Property(a => a.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.EntityName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(a => a.OldValues)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(a => a.NewValues)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(a => a.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.HasIndex(a => new { a.EntityName, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");

        builder.HasIndex(a => new { a.UserId, a.CreatedAtUtc })
            .HasDatabaseName("IX_AuditLogs_User_Date");
    }
}
