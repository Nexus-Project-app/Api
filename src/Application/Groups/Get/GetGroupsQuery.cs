using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Groups.Get;

public sealed record GetGroupsQuery(
    int Page,
    int PageSize,
    Guid? CurrentUserId = null,
    string? Search = null
) : IQuery<PagedList<GroupSummaryResponse>>;
