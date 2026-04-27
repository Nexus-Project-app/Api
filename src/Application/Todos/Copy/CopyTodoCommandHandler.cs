using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Todos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Todos.Copy;

internal sealed class CopyTodoCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<CopyTodoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CopyTodoCommand command, CancellationToken cancellationToken)
    {
        if (userContext.UserId != command.UserId)
        {
            return Result.Failure<Guid>(UserErrors.Unauthorized());
        }

        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(command.UserId));
        }

        TodoItem? existingTodo = await context.TodoItems.AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == command.TodoId && t.UserId == command.UserId, cancellationToken);

        if (existingTodo is null)
        {
            return Result.Failure<Guid>(TodoItemErrors.NotFound(command.TodoId));
        }

        var copiedTodoItem = new TodoItem
        {
            UserId = user.Id,
            Description = existingTodo.Description,
            Priority = existingTodo.Priority,
            DueDate = existingTodo.DueDate,
            Labels = existingTodo.Labels.ToList(), // Create a new list to avoid reference issues
            IsCompleted = false, // Reset completion status for the copy
            CreatedAt = dateTimeProvider.UtcNow
        };

        copiedTodoItem.Raise(new TodoItemCreatedDomainEvent(copiedTodoItem.Id));

        context.TodoItems.Add(copiedTodoItem);

        await context.SaveChangesAsync(cancellationToken);

        return copiedTodoItem.Id;
    }
}
