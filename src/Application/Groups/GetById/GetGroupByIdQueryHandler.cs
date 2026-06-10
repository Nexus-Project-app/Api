using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.GetById;

internal sealed class GetGroupByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetGroupByIdQuery, GroupDetailResponse>
{
    public async Task<Result<GroupDetailResponse>> Handle(GetGroupByIdQuery query, CancellationToken cancellationToken)
    {
        var response = await context.Groups
            .Where(g => g.Id == query.GroupId)
            .Select(g => new GroupDetailResponse
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                Visibility = g.Visibility,
                OwnerId = g.OwnerId,
                OwnerName = (g.Owner.FirstName + " " + g.Owner.LastName).Trim(),
                MemberCount = context.GroupMembers.Count(m => m.GroupId == g.Id),
                Created = g.Created,
                IsMember = query.CurrentUserId.HasValue &&
                    context.GroupMembers.Any(m => m.GroupId == g.Id && m.UserId == query.CurrentUserId.Value),
                CurrentUserRole = query.CurrentUserId.HasValue
                    ? context.GroupMembers
                        .Where(m => m.GroupId == g.Id && m.UserId == query.CurrentUserId.Value)
                        .Select(m => (GroupMemberRole?)m.Role)
                        .FirstOrDefault()
                    : null,
                HasPendingRequest = query.CurrentUserId.HasValue &&
                    context.GroupJoinRequests.Any(r => r.GroupId == g.Id
                        && r.UserId == query.CurrentUserId.Value
                        && r.Status == GroupJoinRequestStatus.Pending)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Result.Failure<GroupDetailResponse>(GroupErrors.NotFound);
        }

        return response;
    }
}
