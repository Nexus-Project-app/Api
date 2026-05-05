using Application.Abstractions.Messaging;

namespace Application.Posts.Delete;

public sealed class DeletePostCommand : ICommand<Guid>
{
    public Guid Id { get; set; }
}
