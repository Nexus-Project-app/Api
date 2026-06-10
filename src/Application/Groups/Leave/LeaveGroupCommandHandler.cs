using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.Leave;

internal sealed class LeaveGroupCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<LeaveGroupCommand>
{
    public async Task<Result> Handle(LeaveGroupCommand command, CancellationToken cancellationToken)
    {
        var member = await context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == command.GroupId && m.UserId == userContext.UserId, cancellationToken);

        if (member is null)
        {
            return Result.Failure(GroupErrors.NotMember);
        }

        if (member.Role == GroupMemberRole.Owner)
        {
            return Result.Failure(GroupErrors.CannotLeaveAsOwner);
        }

        context.GroupMembers.Remove(member);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
