using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Extensions;
using FluentValidation;

namespace BudgetManager.Application.Features.Budget.Unlock;

public sealed class UnlockBudgetCommandValidator : AbstractValidator<UnlockBudgetCommand>
{
    public UnlockBudgetCommandValidator(IBudgetContext budgetContext)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.BudgetIdRequired)
            .MustAsync(budgetContext.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.BudgetNotExists)
            .CustomAsync(async (id, context, cancellationToken)
                => (await budgetContext.IsUnlockableAsync(id, cancellationToken))
                                       .GetBudgetUnlockableStatusError(context));
    }
}
