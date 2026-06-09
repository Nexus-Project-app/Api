using Application.Abstractions.Messaging;
using Application.Common;
using Application.Posts.Get;

namespace Application.Groups.GetPosts;

public sealed record GetGroupPostsQuery(
    Guid GroupId,
    int Page,
    int PageSize,
    Guid? CurrentUserId = null) : IQuery<PagedList<PostResponse>>;
