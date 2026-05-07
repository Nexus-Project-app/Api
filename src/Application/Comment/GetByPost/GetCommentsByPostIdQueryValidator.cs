using FluentValidation;

namespace Application.Comment.GetByPost;

internal sealed class GetCommentsByPostIdQueryValidator : AbstractValidator<GetCommentsByPostIdQuery>
{
    public GetCommentsByPostIdQueryValidator()
    {
        RuleFor(q => q.PostId).NotEmpty();
        RuleFor(q => q.Page).GreaterThan(0);
        RuleFor(q => q.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}