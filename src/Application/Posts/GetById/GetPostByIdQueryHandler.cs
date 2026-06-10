using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Domain.Posts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Posts.GetById;

internal sealed class GetPostByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetPostByIdQuery, PostResponse>
{
    public async Task<Result<PostResponse>> Handle(GetPostByIdQuery query, CancellationToken cancellationToken)
    {
        // Résoudre l'ID DB de l'utilisateur courant à partir du sub Keycloak (si fourni)
        Guid? currentUserId = null;
        if (!string.IsNullOrEmpty(query.CurrentUserSub))
        {
            currentUserId = await context.Users
                .Where(u => u.KeycloakId == query.CurrentUserSub)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var post = await context.Posts
            .Where(p => p.Id == query.PostId && p.Deleted == null)
            .Where(p => p.GroupId == null ||
                        p.Group!.DeletedAt == null &&
                         (p.Group.Visibility == GroupVisibility.Public ||
                          currentUserId.HasValue && context.GroupMembers.Any(
                              m => m.GroupId == p.GroupId && m.UserId == currentUserId.Value)))
            .Select(p => new PostResponse
            {
                Id = p.Id,
                AuthorId = p.AuthorId,
                AuthorName = (p.Author.FirstName + " " + p.Author.LastName).Trim(),
                Title = p.Title,
                Content = p.Content,
                Tags = p.Tags.Select(t => t.Name).ToList(),
                Created = p.Created,
                Updated = p.Updated,
                LikeCount = context.Likes.Count(l => l.PostId == p.Id),
                CommentCount = context.Comments.Count(c => c.PostId == p.Id && c.Deleted == null),
                IsLikedByCurrentUser = currentUserId.HasValue &&
                    context.Likes.Any(l => l.PostId == p.Id && l.AuthorId == currentUserId.Value),
                GroupId = p.GroupId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (post is null)
        {
            return Result.Failure<PostResponse>(PostErrors.NotFound(query.PostId));
        }

        return post;
    }
}
