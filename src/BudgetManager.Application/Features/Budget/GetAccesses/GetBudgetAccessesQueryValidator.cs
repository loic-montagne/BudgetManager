using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.Budget.GetAccesses;

public sealed class GetBudgetAccessesQueryValidator : AbstractValidator<GetBudgetAccessesQuery>
{
    public GetBudgetAccessesQueryValidator(IBudgetContext budgetContext)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.BudgetIdRequired);
    }
}
