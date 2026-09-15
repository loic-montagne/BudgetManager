using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Extensions;
using FluentValidation;

namespace BudgetManager.Application.Features.Budget.Lock;

public sealed class LockBudgetCommandValidator : AbstractValidator<LockBudgetCommand>
{
    public LockBudgetCommandValidator(IBudgetContext budgetContext)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.BudgetIdRequired)
            .MustAsync(budgetContext.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.BudgetNotExists)
            .CustomAsync(async (id, context, cancellationToken)
                => (await budgetContext.IsLockableAsync(id, cancellationToken))
                                       .GetBudgetLockableStatusError(context));
    }
}
