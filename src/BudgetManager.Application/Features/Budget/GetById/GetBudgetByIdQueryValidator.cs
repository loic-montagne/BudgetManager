using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.Budget.GetById;

public sealed class GetBudgetByIdQueryValidator : AbstractValidator<GetBudgetByIdQuery>
{
    public GetBudgetByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.BudgetIdRequired);
    }
}
