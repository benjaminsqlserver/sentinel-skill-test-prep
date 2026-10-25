namespace Sentinel.Contracts;

/// <summary>One page of a longer list. <c>TotalCount</c> lets the UI draw a pager.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Returned by create endpoints alongside a Location header.</summary>
public sealed record CreatedResponse(Guid Id);
