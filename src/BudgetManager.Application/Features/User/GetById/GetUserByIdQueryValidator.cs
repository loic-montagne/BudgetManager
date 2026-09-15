using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.User.GetById;

public sealed class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.UserIdRequired);
    }
}
