# Chapter 5 · Domain Building Blocks

Four small types carry the whole domain: an error, a result, an entity and
an aggregate root. They are worth getting exactly right, because every
other class leans on them.

## 5.1 Errors are values, exceptions are for bugs

There are two kinds of failure. "That email is already registered" happens
every day and is part of the business. "The database connection string is
null" should never happen and means the program is wrong. Mixing them, by
throwing exceptions for both, makes every caller wrap every call in
`try/catch` and guess which is which.

Sentinel's rule: **business failures are returned as `Error` values;
exceptions are reserved for programming errors.**

<!-- include: src/Sentinel.Domain/Common/ErrorType.cs -->

<!-- include: src/Sentinel.Domain/Common/Error.cs -->

An error has a stable `Code` (`User.DuplicateEmail`) that clients and tests
can rely on, a human `Message`, and a `Type` that the API maps to a status
code in exactly one place (Chapter 12). The record is not sealed because
the Application layer derives a `ValidationError` that carries one message
per field.

## 5.2 Result: success or failure, never both

<!-- include: src/Sentinel.Domain/Common/Result.cs -->

Points worth noticing:

- The constructor enforces the invariant. A `Result` that is a success with
  an error, or a failure without one, cannot be constructed, so no caller
  ever has to check for it.
- Reading `Value` from a failure **throws**. Returning `default` would turn
  a forgotten `if (result.IsFailure)` into a silent zero or null somewhere
  far away.
- The implicit conversions make handlers read naturally:
  `return UserErrors.NotFound(id);` and `return user.Id;` both compile in a
  method returning `Result<Guid>`.

::: exam
In a 40-minute coding scenario you will not build a `Result` type. You will
still face the decision it encodes: what does my function return when the
input is bad? Decide explicitly (an empty list, `null`, an exception), say
so in a comment, and test it.
:::

## 5.3 Entities and aggregate roots

<!-- include: src/Sentinel.Domain/Common/Entity.cs -->

Equality by id, and by type: a `User` and a `Role` that happened to share a
Guid are not equal. The protected parameterless constructor exists for
EF Core only.

<!-- include: src/Sentinel.Domain/Common/AggregateRoot.cs -->

An aggregate root is the only entry point to a cluster of objects that must
change together. `User` is a root; `UserRole` and `PermissionOverride` live
inside it and are only changed through `User`'s methods. That is what lets
`User` guarantee, for example, that a role is never assigned twice.

## 5.4 Value objects, briefly

A value object has no identity: two `EmailAddress` instances with the same
text *are* the same email. Sentinel's value objects (`EmailAddress`,
`Permission`, `GeoLocation`) share a pattern:

1. A private constructor.
2. A static `Create` that validates, normalises and returns a `Result`.
3. Equality by value (a `record`, or `IEquatable<T>` written out).

<!-- include: src/Sentinel.Domain/Users/EmailAddress.cs -->

The validation is deliberately shallow. The only true test of an email
address is delivering mail to it; everything else is a heuristic, and a
strict regex rejects real addresses. Note the comment about
`MailAddress.TryCreate` accepting a display name: that is the kind of
library behaviour you only discover by testing edge cases, and the kind of
discovery worth a comment.
