using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Contracts;

namespace Sentinel.Application.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserDetailDto>;
