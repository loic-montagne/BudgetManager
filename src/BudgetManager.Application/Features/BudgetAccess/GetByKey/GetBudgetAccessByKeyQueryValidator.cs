using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.BudgetAccess.GetByKey;

public sealed class GetBudgetAccessByKeyQueryValidator : AbstractValidator<GetBudgetAccessByKeyQuery>
{
    public GetBudgetAccessByKeyQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
                .WithMessage("UserId is required.")
                .WithErrorCode(ErrorCodes.BudgetAccessUserIdRequired);

        RuleFor(x => x.BudgetId)
            .NotEmpty()
                .WithMessage("BudgetId is required.")
                .WithErrorCode(ErrorCodes.BudgetAccessBudgetIdRequired);
    }
}
