using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Posts.Get;

public sealed record GetPostsQuery(
    int Page,
    int PageSize,
    string? CurrentUserSub = null,
    string? Search = null
) : IQuery<PagedList<PostResponse>>;
