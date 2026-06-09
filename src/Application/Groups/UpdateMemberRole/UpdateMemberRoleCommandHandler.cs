using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.UpdateMemberRole;

internal sealed class UpdateMemberRoleCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<UpdateMemberRoleCommand>
{
    public async Task<Result> Handle(UpdateMemberRoleCommand command, CancellationToken cancellationToken)
    {
        if (command.Role == GroupMemberRole.Owner)
        {
            return Result.Failure(GroupErrors.CannotPromoteToOwner);
        }

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

        var target = await context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == command.GroupId && m.UserId == command.UserId, cancellationToken);

        if (target is null)
        {
            return Result.Failure(GroupErrors.NotMember);
        }

        if (target.Role == GroupMemberRole.Owner)
        {
            return Result.Failure(GroupErrors.CannotKickOwner);
        }

        target.Role = command.Role;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
