using SharedKernel;

namespace Domain.Comment;

public static class CommentErrors
{
    public static Error NotFound(Guid commentId) => Error.NotFound(
        "Comments.NotFound",
        $"Le commentaire avec l'id = '{commentId}' n'a pas été trouvé");

    public static Error Deleted(Guid commentId) => Error.Conflict(
        "Comments.Deleted",
        $"Le commentaitre avec l'id = '{commentId}' a été supprimé");

    public static Error Unauthorized() => Error.Failure(
        "Comments.Unauthorized",
        "You are not authorized to perform this action.");
}
