using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using SharedKernel;

namespace Application.Groups.Create;

internal sealed class CreateGroupCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateGroupCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGroupCommand command, CancellationToken cancellationToken)
    {
        var groupId = Guid.NewGuid();
        var now = dateTimeProvider.UtcNow;

        var group = new Group
        {
            Id = groupId,
            Name = command.Name,
            Description = command.Description,
            Visibility = command.Visibility,
            OwnerId = userContext.UserId,
            Created = now
        };

        var ownerMember = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = userContext.UserId,
            Role = GroupMemberRole.Owner,
            JoinedAt = now
        };

        context.Groups.Add(group);
        context.GroupMembers.Add(ownerMember);
        await context.SaveChangesAsync(cancellationToken);

        return groupId;
    }
}
