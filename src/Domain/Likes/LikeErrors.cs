using SharedKernel;

namespace Domain.Likes;

public static class LikeErrors
{
    public static Error AlreadyLiked(Guid postId) =>
        Error.Conflict("Like.AlreadyLiked", $"User has already liked post '{postId}'.");

    public static Error NotFound(Guid postId) =>
        Error.NotFound("Like.NotFound", $"Like for post '{postId}' was not found.");
}
