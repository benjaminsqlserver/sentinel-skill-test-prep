using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sentinel.Domain.Pages;
using Sentinel.Infrastructure.Persistence.Converters;

namespace Sentinel.Infrastructure.Persistence.Configurations;

internal sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("Pages");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Path).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => p.Path).IsUnique();
        builder.Property(p => p.Title).HasMaxLength(Page.MaxTitleLength).IsRequired();
        // The cast picks the non-generic overload; the generic one is fussy about
        // the nullability of Permission? versus Permission.
        builder.Property(p => p.RequiredPermission).HasConversion((ValueConverter)new PermissionConverter()).HasMaxLength(128);

        // A real foreign key: the database refuses orphans. The evaluator
        // still copes with them, because data arrives from places other than
        // this database (an import, a test, a future cache) and the menu must
        // never crash. Belt and braces, on purpose.
        builder.HasOne<Page>()
            .WithMany()
            .HasForeignKey(p => p.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
