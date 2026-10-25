using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sentinel.Domain.Access;
using Sentinel.Domain.Roles;
using Sentinel.Infrastructure.Persistence.Converters;

namespace Sentinel.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).HasMaxLength(Role.MaxNameLength).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.Description).HasMaxLength(500);

        // The public Permissions property is a read-only view; map the
        // private list behind it, as one column.
        builder.Ignore(r => r.Permissions);
        builder.Property<List<Permission>>("_permissions")
            .HasColumnName("Permissions")
            .HasConversion(new PermissionListConverter(), new PermissionListComparer())
            .HasMaxLength(2000)
            .IsRequired();
    }
}
