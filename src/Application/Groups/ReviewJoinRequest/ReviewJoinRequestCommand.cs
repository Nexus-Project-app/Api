using Application.Abstractions.Messaging;

namespace Application.Groups.ReviewJoinRequest;

public sealed record ReviewJoinRequestCommand(
    Guid GroupId,
    Guid RequestId,
    bool Accept) : ICommand;
