using System;
using System.Collections.Generic;
using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Keycloak;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tags;
using Application.Posts.Create;
using Domain.Posts;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Keycloak.SetUserKeycloak;

internal sealed class SetUserKeycloakCommandHandler(
    IApplicationDbContext context,
    //IDateTimeProvider dateTimeProvider,
    IUserContext userContext,
    IKeycloakService keycloakService)
    : ICommandHandler<SetUserKeycloakCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        SetUserKeycloakCommand command,
        CancellationToken cancellationToken)
    {
        //var now = dateTimeProvider.UtcNow;

        var userId = userContext.UserId;

     
        // 1. Update Keycloak
        await keycloakService.UpdateUserAsync(userId, new UpdateKeycloakUserRequest(
            command.FirstName,
            command.LastName
        ));

        // 2. Update DB si tu as un user local
        var user = await context.Users
            .FirstOrDefaultAsync(x => x.KeycloakId == userId.ToString(), cancellationToken);

        if (user == null)
        {
            return Result.Failure<Guid>(
                new Error("User.NotFound", "Utilisateur introuvable", ErrorType.Failure)
            );
        }
        user.FirstName = command.FirstName;
        user.LastName = command.LastName;
        //user.UpdateAt = now;
        
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }
}

//internal sealed class CreatePostCommandHandler(
//    IApplicationDbContext context,
//    IDateTimeProvider dateTimeProvider,
//    IUserContext userContext,
//    ITagService tagService)
//    : ICommandHandler<CreatePostCommand, Guid>
//{
//    public async Task<Result<Guid>> Handle(CreatePostCommand command, CancellationToken cancellationToken)
//    {
//        var now = dateTimeProvider.UtcNow;

//        var tags = await tagService.ResolveTagsAsync(command.Tags, cancellationToken);

//        var post = new Post
//        {
//            Id = Guid.NewGuid(),
//            Title = command.Title,
//            Content = command.Content,
//            AuthorId = userContext.UserId,
//            Tags = tags,
//            Created = now,
//        };

//        post.Raise(new PostCreatedDomainEvent(post.Id));

//        context.Posts.Add(post);
//        await context.SaveChangesAsync(cancellationToken);

//        return post.Id;
//    }
//}

