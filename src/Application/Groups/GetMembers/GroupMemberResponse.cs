using Domain.Groups;

namespace Application.Groups.GetMembers;

public sealed class GroupMemberResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public GroupMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
