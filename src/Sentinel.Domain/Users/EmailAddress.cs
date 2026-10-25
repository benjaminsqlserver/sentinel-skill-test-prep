using System.Net.Mail;
using Sentinel.Domain.Common;

namespace Sentinel.Domain.Users;

/// <summary>
/// An email address, trimmed and lower-cased so that "Ada@Example.com" and
/// "ada@example.com " are the same person. Validation is deliberately
/// shallow: the only real test of an address is sending mail to it.
/// </summary>
public sealed record EmailAddress
{
    public const int MaxLength = 254; // RFC 5321 path limit

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static Result<EmailAddress> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return UserErrors.EmailRequired;

        var normalised = value.Trim().ToLowerInvariant();

        if (normalised.Length > MaxLength)
            return UserErrors.EmailInvalid(value);

        // MailAddress accepts "Display Name <a@b>", so insist that what it
        // parsed is exactly what we were given.
        return MailAddress.TryCreate(normalised, out var parsed) && parsed.Address == normalised && normalised.Contains('.')
            ? new EmailAddress(normalised)
            : UserErrors.EmailInvalid(value);
    }

    public override string ToString() => Value;
}
