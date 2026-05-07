using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Comment.GetByPost;

public sealed record GetCommentsByPostIdQuery(Guid PostId, int Page, int PageSize) : IQuery<PagedList<CommentResponse>>;