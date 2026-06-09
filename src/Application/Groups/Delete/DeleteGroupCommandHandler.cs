using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Groups;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Groups.Delete;

internal sealed class DeleteGroupCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<DeleteGroupCommand>
{
    public async Task<Result> Handle(DeleteGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await context.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId, cancellationToken);

        if (group is null)
        {
            return Result.Failure(GroupErrors.NotFound);
        }

        if (group.OwnerId != userContext.UserId)
        {
            return Result.Failure(GroupErrors.NotAuthorized);
        }

        context.Groups.Remove(group);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
