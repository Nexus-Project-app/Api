using FluentValidation;

namespace Application.Posts.GetByUser;

internal sealed class GetPostsByUserQueryValidator : AbstractValidator<GetPostsByUserQuery>
{
    public GetPostsByUserQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}
