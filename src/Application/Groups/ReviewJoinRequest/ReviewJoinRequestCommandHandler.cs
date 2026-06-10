using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.ReviewJoinRequest;

internal sealed class ReviewJoinRequestCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReviewJoinRequestCommand>
{
    public async Task<Result> Handle(ReviewJoinRequestCommand command, CancellationToken cancellationToken)
    {
        var isModeratorOrOwner = await context.GroupMembers
            .AnyAsync(m => m.GroupId == command.GroupId
                && m.UserId == userContext.UserId
                && (m.Role == GroupMemberRole.Owner || m.Role == GroupMemberRole.Moderator),
                cancellationToken);

        if (!isModeratorOrOwner)
        {
            return Result.Failure(GroupErrors.NotAuthorized);
        }

        var request = await context.GroupJoinRequests
            .FirstOrDefaultAsync(r => r.Id == command.RequestId
                && r.GroupId == command.GroupId
                && r.Status == GroupJoinRequestStatus.Pending,
                cancellationToken);

        if (request is null)
        {
            return Result.Failure(GroupErrors.RequestNotFound);
        }

        var now = dateTimeProvider.UtcNow;
        request.Status = command.Accept ? GroupJoinRequestStatus.Accepted : GroupJoinRequestStatus.Rejected;
        request.ReviewedByUserId = userContext.UserId;
        request.ReviewedAt = now;

        if (command.Accept)
        {
            var member = new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = command.GroupId,
                UserId = request.UserId,
                Role = GroupMemberRole.Member,
                JoinedAt = now
            };
            context.GroupMembers.Add(member);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
