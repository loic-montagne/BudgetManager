using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Contexts;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class ContextBranchCoverageTests
{
    [Fact]
    public async Task AccountContext_WhenAccountDoesNotExist_IsOpenedAndIsClosedReturnFalse()
    {
        var repository = Substitute.For<IAccountRepository>();
        var accountId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .GetTrackedByIdAsync(accountId, cancellationToken)
            .Returns((Account?)null);

        var context = new AccountContext(repository, new EntityCacheContext());

        var isOpened = await context.IsOpenedAsync(accountId, cancellationToken);
        var isClosed = await context.IsClosedAsync(accountId, cancellationToken);

        Assert.False(isOpened);
        Assert.False(isClosed);
    }

    [Fact]
    public async Task BudgetCategoryContext_WhenCategoryExistsButIsNotAssociated_ReturnsFalse()
    {
        var category = BudgetCategory.Create("Category", null);
        var repository = Substitute.For<IBudgetCategoryRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .GetTrackedByIdAsync(category.Id, cancellationToken)
            .Returns(category);

        var context = new BudgetCategoryContext(repository, new EntityCacheContext());

        var result = await context.IsAssociatedToBudgetAsync(
            category.Id,
            Guid.NewGuid(),
            cancellationToken);

        Assert.False(result);
    }
        
    [Fact]
    public async Task TransactionContext_WhenBudgetDoesNotMatch_IsInBudgetReturnsFalseWhileCategoryMatches()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);

        budget.AssociateCategory(category, ownerId);

        var transaction = budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Operation",
            TransactionType.Expense,
            10m,
            PaymentMethod.Cash,
            null,
            ownerId);

        var repository = Substitute.For<ITransactionRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .GetTrackedByIdAsync(transaction.Id, cancellationToken)
            .Returns(transaction);

        var context = new TransactionContext(repository, new EntityCacheContext());

        var isInBudget = await context.IsInBudgetAsync(
            transaction.Id,
            Guid.NewGuid(),
            cancellationToken);

        var isInCategory = await context.IsInCategoryAsync(
            transaction.Id,
            category.Id,
            cancellationToken);

        Assert.False(isInBudget);
        Assert.True(isInCategory);
    }

    [Fact]
    public async Task TransactionContext_WhenCategoryDoesNotMatch_IsInCategoryReturnsFalseWhileBudgetMatches()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);

        budget.AssociateCategory(category, ownerId);

        var transaction = budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Operation",
            TransactionType.Expense,
            10m,
            PaymentMethod.Cash,
            null,
            ownerId);

        var repository = Substitute.For<ITransactionRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .GetTrackedByIdAsync(transaction.Id, cancellationToken)
            .Returns(transaction);

        var context = new TransactionContext(repository, new EntityCacheContext());

        var isInBudget = await context.IsInBudgetAsync(
            transaction.Id,
            budget.Id,
            cancellationToken);

        var isInCategory = await context.IsInCategoryAsync(
            transaction.Id,
            Guid.NewGuid(),
            cancellationToken);

        Assert.True(isInBudget);
        Assert.False(isInCategory);
    }
}
