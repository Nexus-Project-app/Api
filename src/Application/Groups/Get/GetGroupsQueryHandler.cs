using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.Get;

internal sealed class GetGroupsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetGroupsQuery, PagedList<GroupSummaryResponse>>
{
    public async Task<Result<PagedList<GroupSummaryResponse>>> Handle(GetGroupsQuery query, CancellationToken cancellationToken)
    {
        var filtered = context.Groups.AsQueryable();

        if (!string.IsNullOrEmpty(query.Search))
        {
            var pattern = $"%{query.Search}%";
            filtered = filtered.Where(g =>
                EF.Functions.ILike(g.Name, pattern) ||
                EF.Functions.ILike(g.Description, pattern));
        }

        var baseQuery = filtered.OrderByDescending(g => g.Created);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(g => new GroupSummaryResponse
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                Visibility = g.Visibility,
                OwnerName = (g.Owner.FirstName + " " + g.Owner.LastName).Trim(),
                MemberCount = context.GroupMembers.Count(m => m.GroupId == g.Id),
                Created = g.Created,
                IsMember = query.CurrentUserId.HasValue &&
                    context.GroupMembers.Any(m => m.GroupId == g.Id && m.UserId == query.CurrentUserId.Value)
            })
            .ToListAsync(cancellationToken);

        return new PagedList<GroupSummaryResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }
}
