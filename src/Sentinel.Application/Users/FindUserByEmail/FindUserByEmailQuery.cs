using Sentinel.Application.Abstractions.Messaging;

namespace Sentinel.Application.Users.FindUserByEmail;

/// <summary>
/// Resolves the user behind a sign-in. The API's authentication handler
/// calls this once per request to turn an identity into a user id.
/// </summary>
public sealed record FindUserByEmailQuery(string Email) : IQuery<Guid>;
