using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Extensions;
using FluentValidation;

namespace BudgetManager.Application.Features.Transaction.Create;

public sealed class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    private readonly ITransactionRepository _transactionRepository;

    public CreateTransactionCommandValidator(IBudgetContext budgetContext, IBudgetCategoryContext categoryContext, IAccountContext accountContext, ITransactionRepository transactionRepository)
    {
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

        RuleFor(x => x.CategoryId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("CategoryId is required.")
                .WithErrorCode(ErrorCodes.TransactionCategoryRequired)
            .MustAsync(categoryContext.ExistsAsync)
                .WithMessage("CategoryId must exist.")
                .WithErrorCode(ErrorCodes.TransactionCategoryNotExists)
            .MustAsync((command, categoryId, cancellationToken) => categoryContext.IsAssociatedToBudgetAsync(categoryId, command.BudgetId, cancellationToken))
                .When(command => command.BudgetId != Guid.Empty && command.CategoryId != Guid.Empty, ApplyConditionTo.CurrentValidator)
                    .WithMessage("Category is not associated with Budget.")
                    .WithErrorCode(ErrorCodes.TransactionCategoryNotAssociatedWithBudget);

        RuleFor(x => x.AccountId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("AccountId is required.")
                .WithErrorCode(ErrorCodes.TransactionAccountRequired)
            .MustAsync(accountContext.ExistsAsync)
                .WithMessage("AccountId must exist.")
                .WithErrorCode(ErrorCodes.TransactionAccountNotExists)
            .MustAsync(accountContext.IsOpenedAsync)
                .WithMessage("Account must be opened.")
                .WithErrorCode(ErrorCodes.TransactionAccountIsClosed);

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(command => "Name is required.")
                .WithErrorCode(ErrorCodes.TransactionNameRequired)
            .MaximumLength(Domain.Common.StringPropertyLengths.NameLength)
                .WithMessage("Name is too long.")
                .WithErrorCode(ErrorCodes.TransactionNameTooLong)
            .MustAsync(NameMustBeUnique)
                .When(command => command.BudgetId != Guid.Empty && command.CategoryId != Guid.Empty, ApplyConditionTo.CurrentValidator)
                    .WithMessage(command => "Name is already used.")
                    .WithErrorCode(ErrorCodes.TransactionNameAlreadyUsed);

        RuleFor(x => x.Type)
            .IsInEnum()
                .WithMessage(command => "Type is invalid.")
                .WithErrorCode(ErrorCodes.TransactionTypeInvalid);

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
            .NotEqual(command => command.AccountId)
                .When(command => command.Method == Domain.Enums.PaymentMethod.BankTransfer, ApplyConditionTo.CurrentValidator)
                .WithMessage("TransferAccountId cannot be equal to AccountId.")
                .WithErrorCode(ErrorCodes.TransactionTransferAccountEqualToAccount)
            .MustAsync((id, cancellationToken) => accountContext.ExistsAsync(id!.Value, cancellationToken))
                .When(command => command.Method == Domain.Enums.PaymentMethod.BankTransfer, ApplyConditionTo.CurrentValidator)
                .WithMessage("TransferAccountId must exist.")
                .WithErrorCode(ErrorCodes.TransactionTransferAccountNotExists)
            .MustAsync((id, cancellationToken) => accountContext.IsOpenedAsync(id!.Value, cancellationToken))
                .When(command => command.Method == Domain.Enums.PaymentMethod.BankTransfer, ApplyConditionTo.CurrentValidator)
                .WithMessage("TransferAccount must be opened.")
                .WithErrorCode(ErrorCodes.TransactionTransferAccountIsClosed);
    }

    private async Task<bool> NameMustBeUnique(CreateTransactionCommand command, string name, CancellationToken cancellationToken)
    {
        return await _transactionRepository.IsNameUniqueAsync(name, command.BudgetId, command.CategoryId, null, cancellationToken);
    }
}
