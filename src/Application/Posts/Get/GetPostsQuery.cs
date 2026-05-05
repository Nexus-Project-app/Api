using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Posts.Get;

public sealed record GetPostsQuery(int Page, int PageSize) : IQuery<PagedList<PostResponse>>;