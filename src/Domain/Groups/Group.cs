using Domain.Users;
using SharedKernel;

namespace Domain.Groups;

public sealed class Group : Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GroupVisibility Visibility { get; set; }
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public DateTime Created { get; set; }
    public List<GroupMember> Members { get; set; } = [];
    public List<GroupJoinRequest> JoinRequests { get; set; } = [];
}
