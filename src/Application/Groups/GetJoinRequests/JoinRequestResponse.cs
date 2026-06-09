using Domain.Groups;

namespace Application.Groups.GetJoinRequests;

public sealed class JoinRequestResponse
{
    public Guid RequestId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public GroupJoinRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
