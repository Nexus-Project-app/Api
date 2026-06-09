using SharedKernel;

namespace Domain.Groups;

public static class GroupErrors
{
    public static readonly Error NotFound = Error.NotFound("Group.NotFound", "Group not found.");
    public static readonly Error NotMember = Error.Problem("Group.NotMember", "You are not a member of this group.");
    public static readonly Error AlreadyMember = Error.Conflict("Group.AlreadyMember", "You are already a member of this group.");
    public static readonly Error NotAuthorized = Error.Problem("Group.NotAuthorized", "You do not have permission to perform this action.");
    public static readonly Error CannotLeaveAsOwner = Error.Problem("Group.CannotLeaveAsOwner", "Owner cannot leave the group. Transfer ownership or delete the group.");
    public static readonly Error RequestNotFound = Error.NotFound("Group.RequestNotFound", "Join request not found.");
    public static readonly Error RequestAlreadyExists = Error.Conflict("Group.RequestAlreadyExists", "A pending join request already exists.");
    public static readonly Error PublicGroupNoRequest = Error.Problem("Group.PublicGroupNoRequest", "Public groups can be joined directly.");
    public static readonly Error CannotKickOwner = Error.Problem("Group.CannotKickOwner", "Cannot kick the group owner.");
    public static readonly Error CannotPromoteToOwner = Error.Problem("Group.CannotPromoteToOwner", "Cannot promote to Owner role through this action.");
}
