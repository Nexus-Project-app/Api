using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.RequestJoin;

internal sealed class RequestJoinGroupCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RequestJoinGroupCommand>
{
    public async Task<Result> Handle(RequestJoinGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await context.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure(GroupErrors.NotFound);
        }

        if (group.Visibility == GroupVisibility.Public)
        {
            return Result.Failure(GroupErrors.PublicGroupNoRequest);
        }

        var alreadyMember = await context.GroupMembers
            .AnyAsync(m => m.GroupId == command.GroupId && m.UserId == userContext.UserId, cancellationToken);

        if (alreadyMember)
        {
            return Result.Failure(GroupErrors.AlreadyMember);
        }

        var pendingExists = await context.GroupJoinRequests
            .AnyAsync(r => r.GroupId == command.GroupId
                && r.UserId == userContext.UserId
                && r.Status == GroupJoinRequestStatus.Pending,
                cancellationToken);

        if (pendingExists)
        {
            return Result.Failure(GroupErrors.RequestAlreadyExists);
        }

        var request = new GroupJoinRequest
        {
            Id = Guid.NewGuid(),
            GroupId = command.GroupId,
            UserId = userContext.UserId,
            Status = GroupJoinRequestStatus.Pending,
            CreatedAt = dateTimeProvider.UtcNow
        };

        context.GroupJoinRequests.Add(request);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
