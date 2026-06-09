using Domain.Users;
using SharedKernel;

namespace Domain.Groups;

public sealed class GroupJoinRequest : Entity
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public GroupJoinRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
