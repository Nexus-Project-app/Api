using Application.Abstractions.Messaging;

namespace Application.Posts.GetById;

public sealed record GetPostByIdQuery(Guid PostId, string? CurrentUserSub = null) : IQuery<PostResponse>;