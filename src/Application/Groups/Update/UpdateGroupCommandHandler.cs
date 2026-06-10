using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.Update;

internal sealed class UpdateGroupCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<UpdateGroupCommand>
{
    public async Task<Result> Handle(UpdateGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await context.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure(GroupErrors.NotFound);
        }

        var isOwnerOrModerator = await context.GroupMembers
            .AnyAsync(m => m.GroupId == command.GroupId
                && m.UserId == userContext.UserId
                && (m.Role == GroupMemberRole.Owner || m.Role == GroupMemberRole.Moderator),
                cancellationToken);

        if (!isOwnerOrModerator)
        {
            return Result.Failure(GroupErrors.NotAuthorized);
        }

        group.Name = command.Name;
        group.Description = command.Description;
        group.Visibility = command.Visibility;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
