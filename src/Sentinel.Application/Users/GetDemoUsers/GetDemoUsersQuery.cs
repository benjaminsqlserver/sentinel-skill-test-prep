using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Contracts;

namespace Sentinel.Application.Users.GetDemoUsers;

/// <summary>
/// Every user, for the development-only "sign in as" switcher. The endpoint
/// that exposes this is only mapped in the Development environment.
/// </summary>
public sealed record GetDemoUsersQuery : IQuery<IReadOnlyList<DemoUserDto>>;
