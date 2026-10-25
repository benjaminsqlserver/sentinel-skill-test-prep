namespace Sentinel.Domain.Common;

/// <summary>
/// The entry point to a cluster of objects that must change together.
/// Outside code holds references to the root only, and every change goes
/// through a method on the root, which is where the invariants are checked.
/// </summary>
/// <remarks>
/// There are no domain events here. Nothing in Sentinel reacts to a change
/// in another aggregate yet, and an event bus with no subscribers is
/// ceremony. Chapter 3 explains when to add one.
/// </remarks>
public abstract class AggregateRoot : Entity
{
    protected AggregateRoot(Guid id) : base(id) { }
    protected AggregateRoot() { }
}
