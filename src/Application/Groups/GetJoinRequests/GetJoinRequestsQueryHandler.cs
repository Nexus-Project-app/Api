using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.GetJoinRequests;

internal sealed class GetJoinRequestsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetJoinRequestsQuery, List<JoinRequestResponse>>
{
    public async Task<Result<List<JoinRequestResponse>>> Handle(GetJoinRequestsQuery query, CancellationToken cancellationToken)
    {
        var isModeratorOrOwner = await context.GroupMembers
            .AnyAsync(m => m.GroupId == query.GroupId
                && m.UserId == query.CurrentUserId
                && (m.Role == GroupMemberRole.Owner || m.Role == GroupMemberRole.Moderator),
                cancellationToken);

        if (!isModeratorOrOwner)
        {
            return Result.Failure<List<JoinRequestResponse>>(GroupErrors.NotAuthorized);
        }

        var requests = await context.GroupJoinRequests
            .Where(r => r.GroupId == query.GroupId && r.Status == GroupJoinRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new JoinRequestResponse
            {
                RequestId = r.Id,
                UserId = r.UserId,
                UserName = (r.User.FirstName + " " + r.User.LastName).Trim(),
                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return requests;
    }
}
