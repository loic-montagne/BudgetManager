using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.Budget.AssociateCategory;
using BudgetManager.Application.Features.Budget.DissociateCategory;
using BudgetManager.Application.Features.Budget.TransferOwnership;
using BudgetManager.Application.Features.Budget.UpdateAccess;
using BudgetManager.Application.Features.Transaction.Create;
using BudgetManager.Application.Features.Transaction.Update;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class ValidatorEdgeCaseTests
{
    [Fact]
    public async Task UpdateBudgetAccessValidator_WhenIdentifiersAndPermissionsAreInvalid_ReturnsExpectedErrors()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var userContext = Substitute.For<IUserContext>();
        var currentUser = new TestCurrentUser();

        var validator = new UpdateBudgetAccessCommandValidator(
            budgetContext,
            userContext,
            currentUser);

        var command = new UpdateBudgetAccessCommand(
            Guid.Empty,
            Guid.Empty,
            (Permission)128);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.BudgetId)
            .WithErrorCode(ErrorCodes.BudgetIdRequired);

        result
            .ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(ErrorCodes.BudgetUserRequired);

        result
            .ShouldHaveValidationErrorFor(x => x.Permissions)
            .WithErrorCode(ErrorCodes.BudgetPermissionsInvalid);
    }

    [Fact]
    public async Task UpdateBudgetAccessValidator_WhenBudgetDoesNotExist_DoesNotRunDependentRules()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var userContext = Substitute.For<IUserContext>();
        var currentUser = new TestCurrentUser();

        var budgetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext
            .ExistsAsync(
                budgetId,
                cancellationToken)
            .Returns(false);

        userContext
            .ExistsAsync(
                userId,
                cancellationToken)
            .Returns(true);

        var validator = new UpdateBudgetAccessCommandValidator(
            budgetContext,
            userContext,
            currentUser);

        var command = new UpdateBudgetAccessCommand(
            budgetId,
            userId,
            Permission.View);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.BudgetId)
            .WithErrorCode(ErrorCodes.BudgetNotExists);

        await budgetContext
            .DidNotReceive()
            .HasCurrentUserPermissionAsync(
                Arg.Any<Guid>(),
                Arg.Any<Permission>(),
                Arg.Any<CancellationToken>());

        await budgetContext
            .DidNotReceive()
            .IsNotOwnerAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssociateCategoryValidator_WhenCategoryIsAlreadyAssociated_ReturnsExpectedError()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var categoryContext = Substitute.For<IBudgetCategoryContext>();

        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext
            .ExistsAsync(
                budgetId,
                cancellationToken)
            .Returns(true);

        budgetContext
            .IsEditableAsync(
                budgetId,
                cancellationToken)
            .Returns(BudgetEditableStatus.Editable);

        categoryContext
            .ExistsAsync(
                categoryId,
                cancellationToken)
            .Returns(true);

        categoryContext
            .IsAssociatedToBudgetAsync(
                categoryId,
                budgetId,
                cancellationToken)
            .Returns(true);

        var validator = new AssociateCategoryCommandValidator(
            budgetContext,
            categoryContext);

        var command = new AssociateCategoryCommand(
            budgetId,
            categoryId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorCode(ErrorCodes.BudgetBudgetCategoryAssociated);
    }

    [Fact]
    public async Task DissociateCategoryValidator_WhenCategoryDoesNotExist_DoesNotCheckWhetherItIsUsed()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var categoryContext = Substitute.For<IBudgetCategoryContext>();
        var categoryRepository = Substitute.For<IBudgetCategoryRepository>();

        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext
            .ExistsAsync(
                budgetId,
                cancellationToken)
            .Returns(true);

        budgetContext
            .IsEditableAsync(
                budgetId,
                cancellationToken)
            .Returns(BudgetEditableStatus.Editable);

        budgetContext
            .GetAsync(
                budgetId,
                cancellationToken)
            .Returns((Budget?)null);

        categoryContext
            .ExistsAsync(
                categoryId,
                cancellationToken)
            .Returns(false);

        var validator = new DissociateCategoryCommandValidator(
            budgetContext,
            categoryContext,
            categoryRepository);

        var command = new DissociateCategoryCommand(
            budgetId,
            categoryId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorCode(ErrorCodes.BudgetBudgetCategoryNotExists);

        await categoryRepository
            .DidNotReceive()
            .IsUsedAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenPaymentMethodIsBankTransferAndTransferAccountIsMissing_ReturnsExpectedError()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var categoryContext = Substitute.For<IBudgetCategoryContext>();
        var accountContext = Substitute.For<IAccountContext>();
        var transactionRepository = Substitute.For<ITransactionRepository>();

        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext
            .ExistsAsync(
                budgetId,
                cancellationToken)
            .Returns(true);

        budgetContext
            .IsEditableAsync(
                budgetId,
                cancellationToken)
            .Returns(BudgetEditableStatus.Editable);

        categoryContext
            .ExistsAsync(
                categoryId,
                cancellationToken)
            .Returns(true);

        categoryContext
            .IsAssociatedToBudgetAsync(
                categoryId,
                budgetId,
                cancellationToken)
            .Returns(true);

        accountContext
            .ExistsAsync(
                accountId,
                cancellationToken)
            .Returns(true);

        accountContext
            .IsOpenedAsync(
                accountId,
                cancellationToken)
            .Returns(true);

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
            "Transfer",
            TransactionType.Expense,
            10,
            PaymentMethod.BankTransfer,
            null);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.TransferAccountId)
            .WithErrorCode(ErrorCodes.TransactionTransferAccountRequired);
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenTransactionDoesNotExist_DoesNotCheckNameUniqueness()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var accountContext = Substitute.For<IAccountContext>();
        var transactionContext = Substitute.For<ITransactionContext>();
        var transactionRepository = Substitute.For<ITransactionRepository>();

        var budgetId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext
            .ExistsAsync(
                budgetId,
                cancellationToken)
            .Returns(true);

        budgetContext
            .IsEditableAsync(
                budgetId,
                cancellationToken)
            .Returns(BudgetEditableStatus.Editable);

        transactionContext
            .ExistsAsync(
                transactionId,
                cancellationToken)
            .Returns(false);

        transactionContext
            .GetAsync(
                transactionId,
                cancellationToken)
            .Returns((Transaction?)null);

        var validator = new UpdateTransactionCommandValidator(
            budgetContext,
            accountContext,
            transactionContext,
            transactionRepository);

        var command = new UpdateTransactionCommand(
            budgetId,
            transactionId,
            "Name",
            1,
            PaymentMethod.Cash,
            null);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(x => x.TransactionId)
            .WithErrorCode(ErrorCodes.TransactionNotExists);

        await transactionRepository
            .DidNotReceive()
            .IsNameUniqueAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransferBudgetOwnershipValidator_WhenTargetIsOwner_ReturnsExpectedError()
    {
        // Arrange

        var currentUserId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var budgetContext =
            Substitute.For<IBudgetContext>();

        var userContext =
            Substitute.For<IUserContext>();

        var currentUser =
            new TestCurrentUser(
                authenticated: true,
                userId: currentUserId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        budgetContext
            .ExistsAsync(
                budgetId,
                cancellationToken)
            .Returns(true);

        budgetContext
            .IsOwnerAsync(
                budgetId,
                currentUserId,
                cancellationToken)
            .Returns(true);

        budgetContext
            .IsNotOwnerAsync(
                budgetId,
                targetUserId,
                cancellationToken)
            .Returns(false);

        userContext
            .ExistsAsync(
                targetUserId,
                cancellationToken)
            .Returns(true);

        var validator =
            new TransferBudgetOwnershipCommandValidator(
                budgetContext,
                userContext,
                currentUser);

        var command =
            new TransferBudgetOwnershipCommand(
                budgetId,
                targetUserId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(
                item => item.UserId)
            .WithErrorCode(
                ErrorCodes.BudgetUserIsOwner);
    }
}
