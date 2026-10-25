using Sentinel.Domain.Common;

namespace Sentinel.Domain.Security;

/// <summary>
/// The user's answer to a finding: "that was me". Once acknowledged, a
/// finding no longer raises the account's risk level. The finding itself is
/// recomputed on every request, so the acknowledgement is stored by key.
/// </summary>
public sealed class FindingAcknowledgement : AggregateRoot
{
    public const int MaxKeyLength = 128;

    private FindingAcknowledgement(Guid id, Guid userId, string findingKey, DateTime acknowledgedAtUtc) : base(id)
    {
        UserId = userId;
        FindingKey = findingKey;
        AcknowledgedAtUtc = acknowledgedAtUtc;
    }

    // For EF Core.
    private FindingAcknowledgement() { }

    public Guid UserId { get; private set; }
    public string FindingKey { get; private set; } = null!;
    public DateTime AcknowledgedAtUtc { get; private set; }

    public static Result<FindingAcknowledgement> Create(Guid userId, string findingKey, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(findingKey) || findingKey.Length > MaxKeyLength)
            return Error.Validation("Finding.InvalidKey", "A finding key is required.");

        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamps must be UTC.", nameof(nowUtc));

        return new FindingAcknowledgement(Guid.CreateVersion7(), userId, findingKey, nowUtc);
    }
}
