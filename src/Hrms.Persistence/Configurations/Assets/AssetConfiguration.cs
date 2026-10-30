using Hrms.Assets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Assets;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets", t =>
        {
            t.HasCheckConstraint("CK_Assets_Category", "[Category] IN ('Laptop','Monitor','Phone','SIM','IDCard','Other')");
            t.HasCheckConstraint("CK_Assets_Condition", "[Condition] IN ('New','Good','Fair','Damaged')");
            t.HasCheckConstraint("CK_Assets_Status", "[Status] IN ('Available','Assigned','InRepair','Retired')");
        });

        builder.HasKey(a => a.AssetId)
            .HasName("PK_Assets");

        builder.Property(a => a.AssetId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(a => a.AssetTag)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(a => a.AssetTag)
            .IsUnique()
            .HasDatabaseName("UQ_Assets_Tag");

        builder.Property(a => a.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.Category)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.SerialNumber)
            .HasMaxLength(100);

        builder.HasIndex(a => a.SerialNumber)
            .IsUnique()
            .HasFilter("[SerialNumber] IS NOT NULL")
            .HasDatabaseName("UX_Assets_Serial");

        builder.Property(a => a.PurchaseDate)
            .HasColumnType("date");

        builder.Property(a => a.Condition)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValueSql("'New'")
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValueSql("'Available'")
            .IsRequired();

        builder.Property(a => a.Notes)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(a => a.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(a => a.RowVersion)
            .IsRowVersion();

        builder.HasIndex(a => new { a.Status, a.Category })
            .HasDatabaseName("IX_Assets_Status_Category");
    }
}
