namespace Sentinel.Domain.Common;

/// <summary>
/// Something with an identity that outlives changes to its attributes.
/// Two entities are the same entity when their ids are equal.
/// </summary>
public abstract class Entity
{
    protected Entity(Guid id) => Id = id;

    // For EF Core materialisation only.
    protected Entity() { }

    public Guid Id { get; protected init; }

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
