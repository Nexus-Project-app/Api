using Application.Abstractions.Messaging;

namespace Application.Todos.Update;

public sealed record UpdateTodoCommand(
    Guid TodoItemId,
    string Description) : ICommand;
