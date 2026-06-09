using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.GetMembers;

internal sealed class GetGroupMembersQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetGroupMembersQuery, List<GroupMemberResponse>>
{
    public async Task<Result<List<GroupMemberResponse>>> Handle(GetGroupMembersQuery query, CancellationToken cancellationToken)
    {
        var group = await context.Groups
            .FirstOrDefaultAsync(g => g.Id == query.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure<List<GroupMemberResponse>>(GroupErrors.NotFound);
        }

        if (group.Visibility == GroupVisibility.Private)
        {
            var isMember = query.CurrentUserId.HasValue &&
                await context.GroupMembers.AnyAsync(
                    m => m.GroupId == query.GroupId && m.UserId == query.CurrentUserId.Value,
                    cancellationToken);

            if (!isMember)
            {
                return Result.Failure<List<GroupMemberResponse>>(GroupErrors.NotMember);
            }
        }

        var members = await context.GroupMembers
            .Where(m => m.GroupId == query.GroupId)
            .OrderBy(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .Select(m => new GroupMemberResponse
            {
                UserId = m.UserId,
                Name = (m.User.FirstName + " " + m.User.LastName).Trim(),
                Role = m.Role,
                JoinedAt = m.JoinedAt
            })
            .ToListAsync(cancellationToken);

        return members;
    }
}
