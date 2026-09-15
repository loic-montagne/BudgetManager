using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Features.Account.Create;
using BudgetManager.Application.Features.Account.Delete;
using BudgetManager.Application.Features.Account.GetById;
using BudgetManager.Application.Features.Bank.Create;
using BudgetManager.Application.Features.Bank.Delete;
using BudgetManager.Application.Features.Bank.GetById;
using BudgetManager.Application.Features.Budget.Create;
using BudgetManager.Application.Features.Budget.GetById;
using BudgetManager.Application.Features.BudgetCategory.Create;
using BudgetManager.Application.Features.BudgetCategory.GetById;
using BudgetManager.Application.Features.Transaction.Create;
using BudgetManager.Application.Features.Transaction.GetById;
using BudgetManager.Domain.Enums;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class RegressionGuardValidatorTests
{
    [Fact]
    public async Task GetAccountByIdValidator_WhenIdIsEmpty_ReturnsAccountIdRequired()
    {
        var validator = new GetAccountByIdQueryValidator();
        var query = new GetAccountByIdQuery(Guid.Empty);

        var result = await validator.TestValidateAsync(
            query,
            cancellationToken: TestContext.Current.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.AccountIdRequired);
    }

    [Fact]
    public async Task GetBankByIdValidator_WhenIdIsEmpty_ReturnsBankIdRequired()
    {
        var validator = new GetBankByIdQueryValidator();
        var query = new GetBankByIdQuery(Guid.Empty);

        var result = await validator.TestValidateAsync(
            query,
            cancellationToken: TestContext.Current.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.BankIdRequired);
    }

    [Fact]
    public async Task GetBudgetByIdValidator_WhenIdIsEmpty_ReturnsBudgetIdRequired()
    {
        var validator = new GetBudgetByIdQueryValidator();
        var query = new GetBudgetByIdQuery(Guid.Empty);

        var result = await validator.TestValidateAsync(
            query,
            cancellationToken: TestContext.Current.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.BudgetIdRequired);
    }

    [Fact]
    public async Task GetBudgetCategoryByIdValidator_WhenIdIsEmpty_ReturnsBudgetCategoryIdRequired()
    {
        var validator = new GetBudgetCategoryByIdQueryValidator();
        var query = new GetBudgetCategoryByIdQuery(Guid.Empty);

        var result = await validator.TestValidateAsync(
            query,
            cancellationToken: TestContext.Current.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.BudgetCategoryIdRequired);
    }

    [Fact]
    public async Task GetTransactionByIdValidator_WhenIdIsEmpty_ReturnsTransactionIdRequired()
    {
        var validator = new GetTransactionByIdQueryValidator();
        var query = new GetTransactionByIdQuery(Guid.Empty);

        var result = await validator.TestValidateAsync(
            query,
            cancellationToken: TestContext.Current.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.TransactionIdRequired);
    }

    [Fact]
    public async Task CreateAccountValidator_WhenNameAlreadyExists_ReturnsAccountNameAlreadyUsed()
    {
        // Arrange

        var repository = Substitute.For<IAccountRepository>();
        var bankContext = Substitute.For<IBankContext>();
        var bankId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .IsNameUniqueAsync(
                "Account",
                null,
                cancellationToken)
            .Returns(false);

        repository
            .IsIbanUniqueAsync(
                Arg.Any<BudgetManager.Domain.ValueObjects.Iban>(),
                null,
                cancellationToken)
            .Returns(true);

        bankContext
            .ExistsAsync(
                bankId,
                cancellationToken)
            .Returns(true);

        var validator = new CreateAccountCommandValidator(
            repository,
            bankContext);

        var command = new CreateAccountCommand(
            "Account",
            "FR7630006000011234567890189",
            bankId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.AccountNameAlreadyUsed);
    }

    [Fact]
    public async Task CreateBankValidator_WhenNameAlreadyExists_ReturnsBankNameAlreadyUsed()
    {
        // Arrange

        var repository = Substitute.For<IBankRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .IsNameUniqueAsync(
                "Bank",
                null,
                cancellationToken)
            .Returns(false);

        repository
            .IsBicUniqueAsync(
                Arg.Any<BudgetManager.Domain.ValueObjects.Bic>(),
                null,
                cancellationToken)
            .Returns(true);

        var validator = new CreateBankCommandValidator(repository);
        var command = new CreateBankCommand(
            "Bank",
            "BNPAFRPP");

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.BankNameAlreadyUsed);
    }

    [Fact]
    public async Task CreateBudgetValidator_WhenNameAlreadyExists_ReturnsBudgetNameAlreadyUsed()
    {
        // Arrange

        var repository = Substitute.For<IBudgetRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .IsNameUniqueAsync(
                "Budget",
                null,
                cancellationToken)
            .Returns(false);

        var validator = new CreateBudgetCommandValidator(repository);
        var command = new CreateBudgetCommand("Budget");

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.BudgetNameAlreadyUsed);
    }

    [Fact]
    public async Task CreateBudgetCategoryValidator_WhenNameAlreadyExists_ReturnsBudgetCategoryNameAlreadyUsed()
    {
        // Arrange

        var repository = Substitute.For<IBudgetCategoryRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .IsNameUniqueAsync(
                "Category",
                null,
                cancellationToken)
            .Returns(false);

        var validator = new CreateBudgetCategoryCommandValidator(repository);
        var command = new CreateBudgetCategoryCommand(
            "Category",
            null);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ErrorCodes.BudgetCategoryNameAlreadyUsed);
    }

    [Fact]
    public async Task DeleteAccountValidator_WhenAccountDoesNotExist_DoesNotCheckUsage()
    {
        // Arrange

        var accountContext = Substitute.For<IAccountContext>();
        var repository = Substitute.For<IAccountRepository>();
        var accountId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        accountContext
            .ExistsAsync(
                accountId,
                cancellationToken)
            .Returns(false);

        var validator = new DeleteAccountCommandValidator(
            accountContext,
            repository);

        var command = new DeleteAccountCommand(accountId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.AccountNotExists);

        await repository
            .DidNotReceive()
            .IsUsedAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBankValidator_WhenBankDoesNotExist_DoesNotCheckUsage()
    {
        // Arrange

        var bankContext = Substitute.For<IBankContext>();
        var repository = Substitute.For<IBankRepository>();
        var bankId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        bankContext
            .ExistsAsync(
                bankId,
                cancellationToken)
            .Returns(false);

        var validator = new DeleteBankCommandValidator(
            bankContext,
            repository);

        var command = new DeleteBankCommand(bankId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ErrorCodes.BankNotExists);

        await repository
            .DidNotReceive()
            .IsUsedAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenTypeIsInvalid_ReturnsTransactionTypeInvalid()
    {
        var validator = CreateTransactionValidator();
        var command = CreateTransactionCommand(
            type: (TransactionType)42);

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: TestContext.Current.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorCode(ErrorCodes.TransactionTypeInvalid);
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenAmountIsInvalid_ReturnsTransactionAmountInvalid()
    {
        var validator = CreateTransactionValidator();
        var command = CreateTransactionCommand(
            amount: 0m);

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: TestContext.Current.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorCode(ErrorCodes.TransactionAmountInvalid);
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenMethodIsInvalid_ReturnsTransactionMethodInvalid()
    {
        var validator = CreateTransactionValidator();
        var command = CreateTransactionCommand(
            method: (PaymentMethod)42);

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: TestContext.Current.CancellationToken);

        result
            .ShouldHaveValidationErrorFor(x => x.Method)
            .WithErrorCode(ErrorCodes.TransactionMethodInvalid);
    }

    private static CreateTransactionCommandValidator CreateTransactionValidator()
    {
        return new CreateTransactionCommandValidator(
            Substitute.For<IBudgetContext>(),
            Substitute.For<IBudgetCategoryContext>(),
            Substitute.For<IAccountContext>(),
            Substitute.For<ITransactionRepository>());
    }

    private static CreateTransactionCommand CreateTransactionCommand(
        TransactionType type = TransactionType.Expense,
        decimal amount = 10m,
        PaymentMethod method = PaymentMethod.Cash)
    {
        return new CreateTransactionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Operation",
            type,
            amount,
            method,
            null);
    }
}
