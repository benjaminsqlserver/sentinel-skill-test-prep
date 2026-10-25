using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Contracts;

namespace Sentinel.Application.Security.GetAccountSecurityStatus;

/// <summary>Is my account safe? Drives the banner and table on the account security page.</summary>
public sealed record GetAccountSecurityStatusQuery(Guid UserId) : IQuery<AccountSecurityStatusDto>;
