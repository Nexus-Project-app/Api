using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.Delete;

internal sealed class DeleteGroupCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<DeleteGroupCommand>
{
    public async Task<Result> Handle(DeleteGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await context.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure(GroupErrors.NotFound);
        }

        if (group.OwnerId != userContext.UserId)
        {
            return Result.Failure(GroupErrors.NotAuthorized);
        }

        DateTime now = dateTimeProvider.UtcNow;

        await context.Posts
            .Where(p => p.GroupId == group.Id && p.Deleted == null)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Deleted, now), cancellationToken);

        await context.GroupMembers
            .Where(m => m.GroupId == group.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await context.GroupJoinRequests
            .Where(r => r.GroupId == group.Id)
            .ExecuteDeleteAsync(cancellationToken);

        group.DeletedAt = now;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
