using Hrms.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Identity;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.ToTable("AspNetUsers", t =>
        {
            t.HasCheckConstraint("CK_AspNetUsers_Status", "[Status] IN ('Active', 'Inactive')");
        });

        builder.Property(u => u.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(u => u.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(u => u.LastLoginAtUtc)
            .HasColumnType("datetime2(3)");
    }
}
