using SharedKernel;

namespace Domain.Posts;

public static class PostErrors
{
    public static Error NotFound(Guid postId) => Error.NotFound(
        "Posts.NotFound",
        $"Le post avec l'id = '{postId}' n'a pas été trouvé");

    public static Error Deleted(Guid postId) => Error.Conflict(
        "Posts.Deleted",
        $"Le post avec l'id = '{postId}' a été supprimé");

    public static Error Unauthorized() => Error.Failure(
        "Posts.Unauthorized",
        "You are not authorized to perform this action.");
}
