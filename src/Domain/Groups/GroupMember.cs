using Domain.Users;
using SharedKernel;

namespace Domain.Groups;

public sealed class GroupMember : Entity
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public GroupMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
