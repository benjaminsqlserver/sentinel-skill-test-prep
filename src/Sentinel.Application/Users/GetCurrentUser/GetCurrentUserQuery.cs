using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Contracts;

namespace Sentinel.Application.Users.GetCurrentUser;

/// <summary>Who am I, and what may I do? Called once when the Angular shell loads.</summary>
public sealed record GetCurrentUserQuery(Guid UserId) : IQuery<CurrentUserDto>;
