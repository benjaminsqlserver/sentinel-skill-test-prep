using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sentinel.Domain.Security;
using Sentinel.Domain.Users;

namespace Sentinel.Infrastructure.Persistence.Configurations;

internal sealed class SignInAttemptConfiguration : IEntityTypeConfiguration<SignInAttempt>
{
    public void Configure(EntityTypeBuilder<SignInAttempt> builder)
    {
        builder.ToTable("SignInAttempts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.IpAddress).HasMaxLength(45).IsRequired(); // longest IPv6 text form
        builder.Property(a => a.CountryCode).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.UserAgent).HasMaxLength(SignInAttempt.MaxUserAgentLength).IsRequired();
        builder.Property(a => a.DeviceId).HasMaxLength(16).IsRequired();
        builder.Property(a => a.FailureReason).HasConversion<string>().HasMaxLength(32);
        builder.Ignore(a => a.Location);

        // Every read is "this user's attempts, newest first".
        builder.HasIndex(a => new { a.UserId, a.OccurredAtUtc }).IsDescending(false, true);
    }
}

internal sealed class FindingAcknowledgementConfiguration : IEntityTypeConfiguration<FindingAcknowledgement>
{
    public void Configure(EntityTypeBuilder<FindingAcknowledgement> builder)
    {
        builder.ToTable("FindingAcknowledgements");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(a => a.FindingKey).HasMaxLength(FindingAcknowledgement.MaxKeyLength).IsRequired();
        builder.HasIndex(a => new { a.UserId, a.FindingKey }).IsUnique();
    }
}
