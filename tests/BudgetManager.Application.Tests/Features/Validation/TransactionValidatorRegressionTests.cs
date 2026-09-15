using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.Transaction.Create;
using BudgetManager.Application.Features.Transaction.Update;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class TransactionValidatorRegressionTests
{
    [Fact]
    public async Task CreateTransactionValidator_WhenAccountDoesNotExist_ReturnsExpectedError()
    {
        var fixture = CreateCreateFixture();

        fixture.AccountContext
            .ExistsAsync(fixture.AccountId, fixture.CancellationToken)
            .Returns(false);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.AccountId)
            .WithErrorCode(ErrorCodes.TransactionAccountNotExists);

        await fixture.AccountContext
            .DidNotReceive()
            .IsOpenedAsync(fixture.AccountId, fixture.CancellationToken);
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenAccountIsClosed_ReturnsExpectedError()
    {
        var fixture = CreateCreateFixture();

        fixture.AccountContext
            .IsOpenedAsync(fixture.AccountId, fixture.CancellationToken)
            .Returns(false);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.AccountId)
            .WithErrorCode(ErrorCodes.TransactionAccountIsClosed);
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenCategoryDoesNotExist_ReturnsExpectedError()
    {
        var fixture = CreateCreateFixture();

        fixture.CategoryContext
            .ExistsAsync(fixture.CategoryId, fixture.CancellationToken)
            .Returns(false);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorCode(ErrorCodes.TransactionCategoryNotExists);

        await fixture.CategoryContext
            .DidNotReceive()
            .IsAssociatedToBudgetAsync(
                fixture.CategoryId,
                fixture.BudgetId,
                fixture.CancellationToken);
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenCategoryIsNotAssociated_ReturnsExpectedError()
    {
        var fixture = CreateCreateFixture();

        fixture.CategoryContext
            .IsAssociatedToBudgetAsync(
                fixture.CategoryId,
                fixture.BudgetId,
                fixture.CancellationToken)
            .Returns(false);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorCode(ErrorCodes.TransactionCategoryNotAssociatedWithBudget);
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenNameAlreadyExists_ReturnsExpectedError()
    {
        var fixture = CreateCreateFixture();

        fixture.TransactionRepository
            .IsNameUniqueAsync(
                fixture.Command.Name,
                fixture.BudgetId,
                fixture.CategoryId,
                null,
                fixture.CancellationToken)
            .Returns(false);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.TransactionNameAlreadyUsed);
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenTransferAccountEqualsSource_ReturnsExpectedError()
    {
        var fixture = CreateCreateFixture(
            PaymentMethod.BankTransfer,
            useSourceAsTransferAccount: true);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.TransferAccountId)
            .WithErrorCode(ErrorCodes.TransactionTransferAccountEqualToAccount);
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenTransferAccountDoesNotExist_ReturnsExpectedError()
    {
        var fixture = CreateCreateFixture(PaymentMethod.BankTransfer);

        fixture.AccountContext
            .ExistsAsync(fixture.TransferAccountId!.Value, fixture.CancellationToken)
            .Returns(false);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.TransferAccountId)
            .WithErrorCode(ErrorCodes.TransactionTransferAccountNotExists);

        await fixture.AccountContext
            .DidNotReceive()
            .IsOpenedAsync(
                fixture.TransferAccountId.Value,
                fixture.CancellationToken);
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenTransferAccountIsClosed_ReturnsExpectedError()
    {
        var fixture = CreateCreateFixture(PaymentMethod.BankTransfer);

        fixture.AccountContext
            .IsOpenedAsync(fixture.TransferAccountId!.Value, fixture.CancellationToken)
            .Returns(false);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.TransferAccountId)
            .WithErrorCode(ErrorCodes.TransactionTransferAccountIsClosed);
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenNameAlreadyExists_ReturnsExpectedError()
    {
        var fixture = CreateUpdateFixture();

        fixture.TransactionRepository
            .IsNameUniqueAsync(
                fixture.Command.Name,
                fixture.BudgetId,
                fixture.Transaction.CategoryId,
                fixture.Transaction.Id,
                fixture.CancellationToken)
            .Returns(false);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.TransactionNameAlreadyUsed);
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenTransferAccountDoesNotExist_ReturnsExpectedError()
    {
        var fixture = CreateUpdateFixture(PaymentMethod.BankTransfer);

        fixture.AccountContext
            .ExistsAsync(fixture.TransferAccountId!.Value, fixture.CancellationToken)
            .Returns(false);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.TransferAccountId)
            .WithErrorCode(ErrorCodes.TransactionTransferAccountNotExists);
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenTransferAccountIsClosed_ReturnsExpectedError()
    {
        var fixture = CreateUpdateFixture(PaymentMethod.BankTransfer);

        fixture.AccountContext
            .IsOpenedAsync(fixture.TransferAccountId!.Value, fixture.CancellationToken)
            .Returns(false);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.TransferAccountId)
            .WithErrorCode(ErrorCodes.TransactionTransferAccountIsClosed);
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenTransferAccountEqualsSource_ReturnsExpectedError()
    {
        var fixture = CreateUpdateFixture(
            PaymentMethod.BankTransfer,
            useSourceAsTransferAccount: true);

        var result = await fixture.Validator.TestValidateAsync(
            fixture.Command,
            cancellationToken: fixture.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.TransferAccountId)
            .WithErrorCode(ErrorCodes.TransactionTransferAccountEqualToAccount);
    }

    private static CreateFixture CreateCreateFixture(
        PaymentMethod method = PaymentMethod.Cash,
        bool useSourceAsTransferAccount = false)
    {
        var budgetContext = Substitute.For<IBudgetContext>();
        var categoryContext = Substitute.For<IBudgetCategoryContext>();
        var accountContext = Substitute.For<IAccountContext>();
        var transactionRepository = Substitute.For<ITransactionRepository>();

        var cancellationToken = TestContext.Current.CancellationToken;
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        Guid? transferAccountId = method == PaymentMethod.BankTransfer
            ? useSourceAsTransferAccount ? accountId : Guid.NewGuid()
            : null;

        budgetContext
            .ExistsAsync(budgetId, cancellationToken)
            .Returns(true);

        budgetContext
            .IsEditableAsync(budgetId, cancellationToken)
            .Returns(BudgetEditableStatus.Editable);

        categoryContext
            .ExistsAsync(categoryId, cancellationToken)
            .Returns(true);

        categoryContext
            .IsAssociatedToBudgetAsync(categoryId, budgetId, cancellationToken)
            .Returns(true);

        accountContext
            .ExistsAsync(accountId, cancellationToken)
            .Returns(true);

        accountContext
            .IsOpenedAsync(accountId, cancellationToken)
            .Returns(true);

        if (transferAccountId.HasValue &&
            transferAccountId.Value != accountId)
        {
            accountContext
                .ExistsAsync(transferAccountId.Value, cancellationToken)
                .Returns(true);

            accountContext
                .IsOpenedAsync(transferAccountId.Value, cancellationToken)
                .Returns(true);
        }

        transactionRepository
            .IsNameUniqueAsync(
                Arg.Any<string>(),
                budgetId,
                categoryId,
                null,
                cancellationToken)
            .Returns(true);

        var validator = new CreateTransactionCommandValidator(
            budgetContext,
            categoryContext,
            accountContext,
            transactionRepository);

        var command = new CreateTransactionCommand(
            budgetId,
            categoryId,
            accountId,
            "Operation",
            TransactionType.Expense,
            10m,
            method,
            transferAccountId);

        return new CreateFixture(
            validator,
            budgetContext,
            categoryContext,
            accountContext,
            transactionRepository,
            command,
            budgetId,
            categoryId,
            accountId,
            transferAccountId,
            cancellationToken);
    }

    private static UpdateFixture CreateUpdateFixture(
        PaymentMethod method = PaymentMethod.Cash,
        bool useSourceAsTransferAccount = false)
    {
        var budgetContext = Substitute.For<IBudgetContext>();
        var accountContext = Substitute.For<IAccountContext>();
        var transactionContext = Substitute.For<ITransactionContext>();
        var transactionRepository = Substitute.For<ITransactionRepository>();

        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerId = Guid.NewGuid();
        var category = BudgetCategory.Create(
            "Category",
            "Description");
        var sourceAccountId = Guid.NewGuid();
        var budget = Budget.Create(
            "Budget",
            ownerId);

        budget.AssociateCategory(
            category,
            ownerId);

        var transaction = budget.AddTransaction(
            category.Id,
            sourceAccountId,
            "Operation",
            TransactionType.Expense,
            10m,
            PaymentMethod.Cash,
            null,
            ownerId);

        Guid? transferAccountId = method == PaymentMethod.BankTransfer
            ? useSourceAsTransferAccount ? sourceAccountId : Guid.NewGuid()
            : null;

        budgetContext
            .ExistsAsync(budget.Id, cancellationToken)
            .Returns(true);

        budgetContext
            .IsEditableAsync(budget.Id, cancellationToken)
            .Returns(BudgetEditableStatus.Editable);

        budgetContext
            .GetAsync(budget.Id, cancellationToken)
            .Returns(budget);

        transactionContext
            .ExistsAsync(transaction.Id, cancellationToken)
            .Returns(true);

        transactionContext
            .GetAsync(transaction.Id, cancellationToken)
            .Returns(transaction);

        if (transferAccountId.HasValue)
        {
            accountContext
                .ExistsAsync(transferAccountId.Value, cancellationToken)
                .Returns(true);

            accountContext
                .IsOpenedAsync(transferAccountId.Value, cancellationToken)
                .Returns(true);
        }

        transactionRepository
            .IsNameUniqueAsync(
                Arg.Any<string>(),
                budget.Id,
                category.Id,
                transaction.Id,
                cancellationToken)
            .Returns(true);

        var validator = new UpdateTransactionCommandValidator(
            budgetContext,
            accountContext,
            transactionContext,
            transactionRepository);

        var command = new UpdateTransactionCommand(
            budget.Id,
            transaction.Id,
            "Updated operation",
            20m,
            method,
            transferAccountId);

        return new UpdateFixture(
            validator,
            accountContext,
            transactionContext,
            transactionRepository,
            command,
            budget.Id,
            transaction,
            transferAccountId,
            cancellationToken);
    }

    private sealed record CreateFixture(
        CreateTransactionCommandValidator Validator,
        IBudgetContext BudgetContext,
        IBudgetCategoryContext CategoryContext,
        IAccountContext AccountContext,
        ITransactionRepository TransactionRepository,
        CreateTransactionCommand Command,
        Guid BudgetId,
        Guid CategoryId,
        Guid AccountId,
        Guid? TransferAccountId,
        CancellationToken CancellationToken);

    private sealed record UpdateFixture(
        UpdateTransactionCommandValidator Validator,
        IAccountContext AccountContext,
        ITransactionContext TransactionContext,
        ITransactionRepository TransactionRepository,
        UpdateTransactionCommand Command,
        Guid BudgetId,
        Transaction Transaction,
        Guid? TransferAccountId,
        CancellationToken CancellationToken);
}
