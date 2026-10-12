using Hrms.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Identity;

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(r => r.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.Description)
            .HasMaxLength(256);

        builder.Property(r => r.IsSystem)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");
    }
}
