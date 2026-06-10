using Application.Abstractions.Messaging;

namespace Application.Likes.Toggle;

public sealed record ToggleLikeCommand(Guid PostId) : ICommand<ToggleLikeResponse>;
