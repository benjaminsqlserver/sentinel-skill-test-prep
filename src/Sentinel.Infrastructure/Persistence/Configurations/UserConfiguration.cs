using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sentinel.Domain.Users;
using Sentinel.Infrastructure.Persistence.Converters;

namespace Sentinel.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasConversion(email => email.Value, value => EmailAddress.Create(value).Value)
            .HasMaxLength(EmailAddress.MaxLength)
            .IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.DisplayName).HasMaxLength(User.MaxDisplayNameLength).IsRequired();
        builder.Property(u => u.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(u => u.SuspensionReason).HasMaxLength(500);

        // Roles and overrides are part of the aggregate: they have no life
        // outside a user, so they are owned rather than separate entities.
        builder.OwnsMany(u => u.Roles, roles =>
        {
            roles.ToTable("UserRoles");
            roles.WithOwner().HasForeignKey("UserId");
            roles.HasKey("UserId", nameof(UserRole.RoleId));
        });
        builder.Navigation(u => u.Roles).HasField("_roles").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(u => u.Overrides, overrides =>
        {
            overrides.ToTable("UserPermissionOverrides");
            overrides.WithOwner().HasForeignKey("UserId");
            overrides.Property(o => o.Permission).HasConversion(new PermissionConverter()).HasMaxLength(128);
            overrides.Property(o => o.Effect).HasConversion<string>().HasMaxLength(8);
            overrides.HasKey("UserId", nameof(PermissionOverride.Permission));
        });
        builder.Navigation(u => u.Overrides).HasField("_overrides").UsePropertyAccessMode(PropertyAccessMode.Field);

        // RoleIds is computed from Roles; it is not a column.
        builder.Ignore(u => u.RoleIds);
    }
}
