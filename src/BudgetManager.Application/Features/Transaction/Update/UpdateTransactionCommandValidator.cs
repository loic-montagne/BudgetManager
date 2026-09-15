using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Extensions;
using FluentValidation;
using FluentValidation.Results;

namespace BudgetManager.Application.Features.Transaction.Update;

public sealed class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    private readonly ITransactionContext _transactionContext;
    private readonly ITransactionRepository _transactionRepository;

    public UpdateTransactionCommandValidator(IBudgetContext budgetContext, IAccountContext accountContext, ITransactionContext transactionContext, ITransactionRepository transactionRepository)
    {
        _transactionContext = transactionContext;
        _transactionRepository = transactionRepository;

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
                .WithMessage("TransactionId does not exist.")
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

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(command => "Name is required.")
                .WithErrorCode(ErrorCodes.TransactionNameRequired)
            .MaximumLength(Domain.Common.StringPropertyLengths.NameLength)
                .WithMessage("Name is too long.")
                .WithErrorCode(ErrorCodes.TransactionNameTooLong)
            .MustAsync(NameMustBeUnique)
                .WithMessage(command => "Name is already used.")
                .WithErrorCode(ErrorCodes.TransactionNameAlreadyUsed);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
                .WithMessage(command => "Amount must be greater than 0.")
                .WithErrorCode(ErrorCodes.TransactionAmountInvalid);

        RuleFor(x => x.Method)
            .IsInEnum()
                .WithMessage(command => "Method is invalid.")
                .WithErrorCode(ErrorCodes.TransactionMethodInvalid);

        RuleFor(x => x.TransferAccountId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .When(command => command.Method == Domain.Enums.PaymentMethod.BankTransfer, ApplyConditionTo.CurrentValidator)
                .WithMessage("TransferAccountId is required.")
                .WithErrorCode(ErrorCodes.TransactionTransferAccountRequired)
            .MustAsync((id, cancellationToken) => accountContext.ExistsAsync(id!.Value, cancellationToken))
                .When(command => command.Method == Domain.Enums.PaymentMethod.BankTransfer, ApplyConditionTo.CurrentValidator)
                .WithMessage("TransferAccountId must exist.")
                .WithErrorCode(ErrorCodes.TransactionTransferAccountNotExists)
            .MustAsync((id, cancellationToken) => accountContext.IsOpenedAsync(id!.Value, cancellationToken))
                .When(command => command.Method == Domain.Enums.PaymentMethod.BankTransfer, ApplyConditionTo.CurrentValidator)
                .WithMessage("TransferAccount must be opened.")
                .WithErrorCode(ErrorCodes.TransactionTransferAccountIsClosed)
            .CustomAsync(async (id, context, cancellationToken) =>
            {
                if (context.InstanceToValidate.Method == Domain.Enums.PaymentMethod.BankTransfer)
                {
                    var t = await transactionContext.GetAsync(context.InstanceToValidate.TransactionId, cancellationToken);
                    if (t?.AccountId == id)
                    {
                        context.AddFailure(new ValidationFailure(context.PropertyPath, "TransferAccountId cannot be equal to AccountId.")
                        {
                            ErrorCode = ErrorCodes.TransactionTransferAccountEqualToAccount
                        });
                    }
                }
            });

    }

    private async Task<bool> NameMustBeUnique(UpdateTransactionCommand command, string name, CancellationToken cancellationToken)
    {
        var transaction = await _transactionContext.GetAsync(command.TransactionId, cancellationToken);
        if (transaction is null)
            return true;
        var categoryId = transaction.CategoryId;
        return await _transactionRepository.IsNameUniqueAsync(name, command.BudgetId, categoryId, command.TransactionId, cancellationToken);
    }
}
