using Sentinel.Application.Abstractions.Messaging;
using Sentinel.Contracts;

namespace Sentinel.Application.Pages.GetPages;

public sealed record GetPagesQuery : IQuery<IReadOnlyList<PageDto>>;
