using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Posts.GetByUser;

public sealed record GetPostsByUserQuery(Guid UserId, int Page, int PageSize) : IQuery<PagedList<PostResponse>>;
