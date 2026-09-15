using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Extensions;
using FluentValidation;
using FluentValidation.Results;

namespace BudgetManager.Application.Features.Transaction.Delete;

public sealed class DeleteTransactionCommandValidator : AbstractValidator<DeleteTransactionCommand>
{
    public DeleteTransactionCommandValidator(IBudgetContext budgetContext, ITransactionContext transactionContext)
    {
        RuleFor(x => x.BudgetId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("BudgetId is required.")
                .WithErrorCode(ErrorCodes.BudgetIdRequired)
            .MustAsync(budgetContext.ExistsAsync)
                .WithMessage("BudgetId does not exist.")
                .WithErrorCode(ErrorCodes.BudgetNotExists)
            .CustomAsync(async (id, context, cancellationToken)
                => (await budgetContext.IsEditableAsync(id, cancellationToken))
                                       .GetBudgetEditableStatusError());

        RuleFor(x => x.TransactionId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("TransactionId is required.")
                .WithErrorCode(ErrorCodes.TransactionIdRequired)
            .MustAsync(transactionContext.ExistsAsync)
                .WithMessage("TransactionId must exist.")
                .WithErrorCode(ErrorCodes.TransactionNotExists);

        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
            {
                var budget = await budgetContext.GetAsync(
                    command.BudgetId,
                    cancellationToken);

                var transaction = await transactionContext.GetAsync(
                    command.TransactionId,
                    cancellationToken);

                if (budget is null || transaction is null)
                    return;

                if (transaction.BudgetId != budget.Id)
                {
                    context.AddFailure(new ValidationFailure(
                        nameof(command.TransactionId),
                        "TransactionId must exist in budget.")
                    {
                        ErrorCode = ErrorCodes.TransactionNotExistsInBudget
                    });
                }
            });
    }
}
