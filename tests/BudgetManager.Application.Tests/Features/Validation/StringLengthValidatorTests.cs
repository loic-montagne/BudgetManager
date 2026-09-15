using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Features.Account.Create;
using BudgetManager.Application.Features.Account.Update;
using BudgetManager.Application.Features.Bank.Create;
using BudgetManager.Application.Features.Bank.Update;
using BudgetManager.Application.Features.Budget.Create;
using BudgetManager.Application.Features.Budget.Update;
using BudgetManager.Application.Features.BudgetCategory.Create;
using BudgetManager.Application.Features.BudgetCategory.Update;
using BudgetManager.Application.Features.Transaction.Create;
using BudgetManager.Application.Features.Transaction.Update;
using BudgetManager.Domain.Enums;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class StringLengthValidatorTests
{
    [Fact]
    public async Task CreateAccountValidator_WhenNameIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var accountRepository =
            Substitute.For<IAccountRepository>();

        var bankContext =
            Substitute.For<IBankContext>();

        var name = new string(
            'A',
            Domain.Common.StringPropertyLengths.NameLength + 1);

        var command = new CreateAccountCommand(
            name,
            "FR7630006000011234567890189",
            Guid.NewGuid());

        var validator =
            new CreateAccountCommandValidator(
                accountRepository,
                bankContext);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Name)
            .WithErrorCode(
                ErrorCodes.AccountNameTooLong);
    }

    [Fact]
    public async Task UpdateAccountValidator_WhenNameIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var accountContext =
            Substitute.For<IAccountContext>();

        var accountRepository =
            Substitute.For<IAccountRepository>();

        var name = new string(
            'A',
            Domain.Common.StringPropertyLengths.NameLength + 1);

        var command = new UpdateAccountCommand(
            Guid.NewGuid(),
            name);

        var validator =
            new UpdateAccountCommandValidator(
                accountContext,
                accountRepository);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Name)
            .WithErrorCode(
                ErrorCodes.AccountNameTooLong);
    }

    [Fact]
    public async Task CreateBankValidator_WhenNameIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var bankRepository =
            Substitute.For<IBankRepository>();

        var name = new string(
            'B',
            Domain.Common.StringPropertyLengths.NameLength + 1);

        var command = new CreateBankCommand(
            name,
            "BNPAFRPP");

        var validator =
            new CreateBankCommandValidator(
                bankRepository);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Name)
            .WithErrorCode(
                ErrorCodes.BankNameTooLong);
    }

    [Fact]
    public async Task UpdateBankValidator_WhenNameIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var bankContext =
            Substitute.For<IBankContext>();

        var bankRepository =
            Substitute.For<IBankRepository>();

        var name = new string(
            'B',
            Domain.Common.StringPropertyLengths.NameLength + 1);

        var command = new UpdateBankCommand(
            Guid.NewGuid(),
            name,
            "BNPAFRPP");

        var validator =
            new UpdateBankCommandValidator(
                bankContext,
                bankRepository);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Name)
            .WithErrorCode(
                ErrorCodes.BankNameTooLong);
    }

    [Fact]
    public async Task CreateBudgetValidator_WhenNameIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var budgetRepository =
            Substitute.For<IBudgetRepository>();

        var name = new string(
            'B',
            Domain.Common.StringPropertyLengths.NameLength + 1);

        var command =
            new CreateBudgetCommand(
                name);

        var validator =
            new CreateBudgetCommandValidator(
                budgetRepository);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Name)
            .WithErrorCode(
                ErrorCodes.BudgetNameTooLong);
    }

    [Fact]
    public async Task UpdateBudgetValidator_WhenNameIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var budgetRepository =
            Substitute.For<IBudgetRepository>();

        var budgetContext =
            Substitute.For<IBudgetContext>();

        var name = new string(
            'B',
            Domain.Common.StringPropertyLengths.NameLength + 1);

        var command = new UpdateBudgetCommand(
            Guid.NewGuid(),
            name);

        var validator =
            new UpdateBudgetCommandValidator(
                budgetRepository,
                budgetContext);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Name)
            .WithErrorCode(
                ErrorCodes.BudgetNameTooLong);
    }

    [Fact]
    public async Task CreateBudgetCategoryValidator_WhenNameIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var budgetCategoryRepository =
            Substitute.For<IBudgetCategoryRepository>();

        var name = new string(
            'C',
            Domain.Common.StringPropertyLengths.NameLength + 1);

        var command =
            new CreateBudgetCategoryCommand(
                name,
                null);

        var validator =
            new CreateBudgetCategoryCommandValidator(
                budgetCategoryRepository);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Name)
            .WithErrorCode(
                ErrorCodes.BudgetCategoryNameTooLong);
    }

    [Fact]
    public async Task CreateBudgetCategoryValidator_WhenDescriptionIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var budgetCategoryRepository =
            Substitute.For<IBudgetCategoryRepository>();

        var description = new string(
            'D',
            Domain.Common.StringPropertyLengths.DescriptionLength + 1);

        var command =
            new CreateBudgetCategoryCommand(
                "Catégorie",
                description);

        var validator =
            new CreateBudgetCategoryCommandValidator(
                budgetCategoryRepository);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Description)
            .WithErrorCode(
                ErrorCodes.BudgetCategoryDescriptionTooLong);
    }

    [Fact]
    public async Task UpdateBudgetCategoryValidator_WhenNameIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var budgetCategoryContext =
            Substitute.For<IBudgetCategoryContext>();

        var budgetCategoryRepository =
            Substitute.For<IBudgetCategoryRepository>();

        var name = new string(
            'C',
            Domain.Common.StringPropertyLengths.NameLength + 1);

        var command =
            new UpdateBudgetCategoryCommand(
                Guid.NewGuid(),
                name,
                null);

        var validator =
            new UpdateBudgetCategoryValidator(
                budgetCategoryContext,
                budgetCategoryRepository);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Name)
            .WithErrorCode(
                ErrorCodes.BudgetCategoryNameTooLong);
    }

    [Fact]
    public async Task UpdateBudgetCategoryValidator_WhenDescriptionIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var budgetCategoryContext =
            Substitute.For<IBudgetCategoryContext>();

        var budgetCategoryRepository =
            Substitute.For<IBudgetCategoryRepository>();

        var description = new string(
            'D',
            Domain.Common.StringPropertyLengths.DescriptionLength + 1);

        var command =
            new UpdateBudgetCategoryCommand(
                Guid.NewGuid(),
                "Catégorie",
                description);

        var validator =
            new UpdateBudgetCategoryValidator(
                budgetCategoryContext,
                budgetCategoryRepository);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Description)
            .WithErrorCode(
                ErrorCodes.BudgetCategoryDescriptionTooLong);
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenNameIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var budgetContext =
            Substitute.For<IBudgetContext>();

        var budgetCategoryContext =
            Substitute.For<IBudgetCategoryContext>();

        var accountContext =
            Substitute.For<IAccountContext>();

        var transactionRepository =
            Substitute.For<ITransactionRepository>();

        var name = new string(
            'T',
            Domain.Common.StringPropertyLengths.NameLength + 1);

        var command =
            new CreateTransactionCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                name,
                TransactionType.Expense,
                10m,
                PaymentMethod.Cash,
                null);

        var validator =
            new CreateTransactionCommandValidator(
                budgetContext,
                budgetCategoryContext,
                accountContext,
                transactionRepository);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Name)
            .WithErrorCode(
                ErrorCodes.TransactionNameTooLong);
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenNameIsTooLong_ReturnsExpectedError()
    {
        // Arrange

        var budgetContext =
            Substitute.For<IBudgetContext>();

        var accountContext =
            Substitute.For<IAccountContext>();

        var transactionContext =
            Substitute.For<ITransactionContext>();

        var transactionRepository =
            Substitute.For<ITransactionRepository>();

        var name = new string(
            'T',
            Domain.Common.StringPropertyLengths.NameLength + 1);

        var command =
            new UpdateTransactionCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                name,
                10m,
                PaymentMethod.Cash,
                null);

        var validator =
            new UpdateTransactionCommandValidator(
                budgetContext,
                accountContext,
                transactionContext,
                transactionRepository);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await validator.TestValidateAsync(
                command,
                cancellationToken:
                    cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                x => x.Name)
            .WithErrorCode(
                ErrorCodes.TransactionNameTooLong);
    }
}
