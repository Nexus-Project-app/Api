using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.KickMember;

internal sealed class KickMemberCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<KickMemberCommand>
{
    public async Task<Result> Handle(KickMemberCommand command, CancellationToken cancellationToken)
    {
        var requester = await context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == command.GroupId && m.UserId == userContext.UserId, cancellationToken);

        if (requester is null || requester.Role != GroupMemberRole.Owner && requester.Role != GroupMemberRole.Moderator)
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

        if (requester.Role == GroupMemberRole.Moderator && target.Role == GroupMemberRole.Moderator)
        {
            return Result.Failure(GroupErrors.NotAuthorized);
        }

        context.GroupMembers.Remove(target);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
