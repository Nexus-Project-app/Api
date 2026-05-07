using SharedKernel;

namespace Domain.Comment;

public static class CommentErrors
{
    public static Error NotFound(Guid commentId) => Error.NotFound(
        "Posts.NotFound",
        $"Le commentaire avec l'id = '{commentId}' n'a pas été trouvé");

    public static Error Deleted(Guid commentId) => Error.Conflict(
        "Posts.Deleted",
        $"Le commentaitre avec l'id = '{commentId}' a été supprimé");

    public static Error Unauthorized() => Error.Failure(
        "Posts.Unauthorized",
        "You are not authorized to perform this action.");
}
