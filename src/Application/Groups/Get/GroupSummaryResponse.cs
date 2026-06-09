using Domain.Groups;

namespace Application.Groups.Get;

public sealed class GroupSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GroupVisibility Visibility { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public DateTime Created { get; set; }
    public bool IsMember { get; set; }
}
