using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Posts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Posts.Delete;

internal sealed class DeletePostCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider
    ) :
    ICommandHandler<DeletePostCommand, Guid>
{
    public async Task<Result<Guid>> Handle(DeletePostCommand command, CancellationToken cancellationToken)
    {
        DateTime now = dateTimeProvider.UtcNow;
        
        Post? post = await context.Posts
            .Include(p => p.Tags)
            .SingleOrDefaultAsync(p => p.Id == command.Id, cancellationToken);
       
        if (post is null)
        {
            return Result.Failure<Guid>(PostErrors.NotFound(command.Id));
        }

        if (post.Deleted is not null)
        {
            return Result.Failure<Guid>(PostErrors.Deleted(command.Id));
        }
        
        post.Deleted = now; await context.SaveChangesAsync(cancellationToken);
        
        return post.Id;
    }
}
