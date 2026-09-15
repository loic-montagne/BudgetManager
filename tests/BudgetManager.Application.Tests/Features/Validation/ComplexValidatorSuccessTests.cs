using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.Budget.AssociateCategory;
using BudgetManager.Application.Features.Budget.DissociateCategory;
using BudgetManager.Application.Features.Budget.TransferOwnership;
using BudgetManager.Application.Features.Budget.UpdateAccess;
using BudgetManager.Application.Features.Transaction.Create;
using BudgetManager.Application.Features.Transaction.Delete;
using BudgetManager.Application.Features.Transaction.Update;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class ComplexValidatorSuccessTests
{
    [Fact]
    public async Task UpdateBudgetAccessValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var userContext = Substitute.For<IUserContext>();
        var currentUserId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(true, currentUserId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext
            .HasCurrentUserPermissionAsync(budgetId, Permission.Share, cancellationToken)
            .Returns(true);
        budgetContext
            .IsNotOwnerAsync(budgetId, targetUserId, cancellationToken)
            .Returns(true);
        userContext.ExistsAsync(targetUserId, cancellationToken).Returns(true);

        var validator = new UpdateBudgetAccessCommandValidator(
            budgetContext,
            userContext,
            currentUser);

        var command = new UpdateBudgetAccessCommand(
            budgetId,
            targetUserId,
            Permission.View | Permission.Edit);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task TransferBudgetOwnershipValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var userContext = Substitute.For<IUserContext>();
        var currentUserId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(true, currentUserId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsOwnerAsync(budgetId, currentUserId, cancellationToken).Returns(true);
        budgetContext.IsNotOwnerAsync(budgetId, targetUserId, cancellationToken).Returns(true);
        userContext.ExistsAsync(targetUserId, cancellationToken).Returns(true);

        var validator = new TransferBudgetOwnershipCommandValidator(
            budgetContext,
            userContext,
            currentUser);

        var command = new TransferBudgetOwnershipCommand(
            budgetId,
            targetUserId);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task AssociateCategoryValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var budgetContext = Substitute.For<IBudgetContext>();
        var categoryContext = Substitute.For<IBudgetCategoryContext>();
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budgetId, cancellationToken).Returns(BudgetEditableStatus.Editable);
        categoryContext.ExistsAsync(categoryId, cancellationToken).Returns(true);
        categoryContext
            .IsAssociatedToBudgetAsync(categoryId, budgetId, cancellationToken)
            .Returns(false);

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

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DissociateCategoryValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);
        budget.AssociateCategory(category, ownerId);

        var budgetContext = Substitute.For<IBudgetContext>();
        var categoryContext = Substitute.For<IBudgetCategoryContext>();
        var categoryRepository = Substitute.For<IBudgetCategoryRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budget.Id, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budget.Id, cancellationToken).Returns(BudgetEditableStatus.Editable);
        budgetContext.GetAsync(budget.Id, cancellationToken).Returns(budget);
        categoryContext.ExistsAsync(category.Id, cancellationToken).Returns(true);
        categoryContext.GetAsync(category.Id, cancellationToken).Returns(category);
        categoryContext
            .IsAssociatedToBudgetAsync(category.Id, budget.Id, cancellationToken)
            .Returns(true);
        categoryRepository
            .IsUsedAsync(category.Id, budget.Id, cancellationToken)
            .Returns(false);

        var validator = new DissociateCategoryCommandValidator(
            budgetContext,
            categoryContext,
            categoryRepository);

        var command = new DissociateCategoryCommand(
            budget.Id,
            category.Id);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateTransactionValidator_WhenCommandIsValid_HasNoErrors()
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

        budgetContext.ExistsAsync(budgetId, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budgetId, cancellationToken).Returns(BudgetEditableStatus.Editable);
        categoryContext.ExistsAsync(categoryId, cancellationToken).Returns(true);
        categoryContext
            .IsAssociatedToBudgetAsync(categoryId, budgetId, cancellationToken)
            .Returns(true);
        accountContext.ExistsAsync(accountId, cancellationToken).Returns(true);
        accountContext.IsOpenedAsync(accountId, cancellationToken).Returns(true);
        transactionRepository
            .IsNameUniqueAsync("Transaction", budgetId, categoryId, null, cancellationToken)
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
            "Transaction",
            TransactionType.Expense,
            10,
            PaymentMethod.Cash,
            null);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateTransactionValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);
        budget.AssociateCategory(category, ownerId);
        var transaction = budget.AddTransaction(
            category.Id,
            accountId,
            "Old transaction",
            TransactionType.Expense,
            10,
            PaymentMethod.Cash,
            null,
            ownerId);

        var budgetContext = Substitute.For<IBudgetContext>();
        var accountContext = Substitute.For<IAccountContext>();
        var transactionContext = Substitute.For<ITransactionContext>();
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budget.Id, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budget.Id, cancellationToken).Returns(BudgetEditableStatus.Editable);
        budgetContext.GetAsync(budget.Id, cancellationToken).Returns(budget);
        transactionContext.ExistsAsync(transaction.Id, cancellationToken).Returns(true);
        transactionContext.GetAsync(transaction.Id, cancellationToken).Returns(transaction);
        transactionRepository
            .IsNameUniqueAsync("New transaction", budget.Id, category.Id, transaction.Id, cancellationToken)
            .Returns(true);

        var validator = new UpdateTransactionCommandValidator(
            budgetContext,
            accountContext,
            transactionContext,
            transactionRepository);

        var command = new UpdateTransactionCommand(
            budget.Id,
            transaction.Id,
            "New transaction",
            20,
            PaymentMethod.Cash,
            null);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteTransactionValidator_WhenCommandIsValid_HasNoErrors()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);
        budget.AssociateCategory(category, ownerId);
        var transaction = budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Transaction",
            TransactionType.Expense,
            10,
            PaymentMethod.Cash,
            null,
            ownerId);

        var budgetContext = Substitute.For<IBudgetContext>();
        var transactionContext = Substitute.For<ITransactionContext>();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.ExistsAsync(budget.Id, cancellationToken).Returns(true);
        budgetContext.IsEditableAsync(budget.Id, cancellationToken).Returns(BudgetEditableStatus.Editable);
        budgetContext.GetAsync(budget.Id, cancellationToken).Returns(budget);
        transactionContext.ExistsAsync(transaction.Id, cancellationToken).Returns(true);
        transactionContext.GetAsync(transaction.Id, cancellationToken).Returns(transaction);

        var validator = new DeleteTransactionCommandValidator(
            budgetContext,
            transactionContext);

        var command = new DeleteTransactionCommand(
            budget.Id,
            transaction.Id);

        // Act

        var result = await validator.TestValidateAsync(
            command,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }
}
