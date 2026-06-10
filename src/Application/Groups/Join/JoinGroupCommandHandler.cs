using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.Join;

internal sealed class JoinGroupCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<JoinGroupCommand>
{
    public async Task<Result> Handle(JoinGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await context.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure(GroupErrors.NotFound);
        }

        if (group.Visibility == GroupVisibility.Private)
        {
            return Result.Failure(GroupErrors.PublicGroupNoRequest);
        }

        var alreadyMember = await context.GroupMembers
            .AnyAsync(m => m.GroupId == command.GroupId && m.UserId == userContext.UserId, cancellationToken);

        if (alreadyMember)
        {
            return Result.Failure(GroupErrors.AlreadyMember);
        }

        var member = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = command.GroupId,
            UserId = userContext.UserId,
            Role = GroupMemberRole.Member,
            JoinedAt = dateTimeProvider.UtcNow
        };

        context.GroupMembers.Add(member);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
