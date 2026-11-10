using Hrms.Assets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Assets;

public class AssetRequestConfiguration : IEntityTypeConfiguration<AssetRequest>
{
    public void Configure(EntityTypeBuilder<AssetRequest> builder)
    {
        builder.ToTable("AssetRequests");

        builder.HasKey(r => r.RequestId)
            .HasName("PK_AssetRequests");

        builder.Property(r => r.RequestId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.AssetId)
            .IsRequired();

        builder.Property(r => r.EmployeeId)
            .IsRequired();

        builder.Property(r => r.IssueType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.AdminNotes)
            .HasMaxLength(1000);

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnType("datetime2(7)")
            .IsRequired();

        builder.Property(r => r.ResolvedAtUtc)
            .HasColumnType("datetime2(7)");

        builder.HasOne(r => r.Asset)
            .WithMany()
            .HasForeignKey(r => r.AssetId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AssetRequests_Assets");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_AssetRequests_Status");

        builder.HasIndex(r => r.AssetId)
            .HasDatabaseName("IX_AssetRequests_AssetId");

        builder.HasIndex(r => r.EmployeeId)
            .HasDatabaseName("IX_AssetRequests_EmployeeId");
    }
}
