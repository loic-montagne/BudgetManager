using BudgetManager.Application.Common;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Contexts;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class ContextRegressionGuardTests
{
    [Fact]
    public async Task BankContext_WhenEntityIsRequestedTwice_UsesCache()
    {
        // Arrange

        var bankRepository = Substitute.For<IBankRepository>();
        var cache = new EntityCacheContext();
        var bank = Bank.Create(
            "Bank",
            Bic.Create("BNPAFRPP"));
        var cancellationToken = TestContext.Current.CancellationToken;

        bankRepository
            .GetTrackedByIdAsync(
                bank.Id,
                cancellationToken)
            .Returns(bank);

        var context = new BankContext(
            bankRepository,
            cache);

        // Act

        var first = await context.GetAsync(
            bank.Id,
            cancellationToken);

        var second = await context.GetAsync(
            bank.Id,
            cancellationToken);

        // Assert

        Assert.Same(bank, first);
        Assert.Same(bank, second);

        await bankRepository
            .Received(1)
            .GetTrackedByIdAsync(
                bank.Id,
                cancellationToken);
    }

    [Fact]
    public async Task UserContext_WhenUserIsRequestedTwice_UsesCache()
    {
        // Arrange

        var userQueries = Substitute.For<IUserQueries>();
        var cache = new EntityCacheContext();
        var user = new Features.User.GetById.UserDto(
            Guid.NewGuid(),
            "user",
            "Doe",
            "John",
            "user@example.test",
            true,
            SupportedCultures.French,
            null,
            null,
            null);
        var cancellationToken = TestContext.Current.CancellationToken;

        userQueries
            .GetByIdAsync(
                user.Id,
                cancellationToken)
            .Returns(user);

        var context = new UserContext(
            userQueries,
            cache);

        // Act

        var first = await context.GetAsync(
            user.Id,
            cancellationToken);

        var second = await context.GetAsync(
            user.Id,
            cancellationToken);

        // Assert

        Assert.Same(user, first);
        Assert.Same(user, second);

        await userQueries
            .Received(1)
            .GetByIdAsync(
                user.Id,
                cancellationToken);
    }

    [Fact]
    public async Task UserContext_WhenUserExists_ExistsAsyncReturnsTrue()
    {
        // Arrange

        var userQueries = Substitute.For<IUserQueries>();
        var cache = new EntityCacheContext();
        var user = new Features.User.GetById.UserDto(
            Guid.NewGuid(),
            "user",
            "Doe",
            "John",
            "user@example.test",
            true,
            SupportedCultures.French,
            null,
            null,
            null);
        var cancellationToken = TestContext.Current.CancellationToken;

        userQueries
            .GetByIdAsync(
                user.Id,
                cancellationToken)
            .Returns(user);

        var context = new UserContext(
            userQueries,
            cache);

        // Act

        var exists = await context.ExistsAsync(
            user.Id,
            cancellationToken);

        // Assert

        Assert.True(exists);
    }

    [Fact]
    public async Task UserContext_WhenUserDoesNotExist_GetRequiredAsyncThrowsNotFoundException()
    {
        // Arrange

        var userQueries = Substitute.For<IUserQueries>();
        var cache = new EntityCacheContext();
        var userId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        userQueries
            .GetByIdAsync(
                userId,
                cancellationToken)
            .Returns((Features.User.GetById.UserDto?)null);

        var context = new UserContext(
            userQueries,
            cache);

        // Act

        var action = () => context.GetRequiredAsync(
            userId,
            cancellationToken);

        // Assert

        await Assert.ThrowsAsync<NotFoundException<BudgetManager.Application.Features.User.GetById.UserDto>>(action);
    }

    [Fact]
    public async Task BudgetCategoryContext_WhenCategoryDoesNotExist_IsAssociatedToBudgetReturnsFalse()
    {
        // Arrange

        var repository = Substitute.For<IBudgetCategoryRepository>();
        var cache = new EntityCacheContext();
        var categoryId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .GetTrackedByIdAsync(
                categoryId,
                cancellationToken)
            .Returns((BudgetCategory?)null);

        var context = new BudgetCategoryContext(
            repository,
            cache);

        // Act

        var result = await context.IsAssociatedToBudgetAsync(
            categoryId,
            budgetId,
            cancellationToken);

        // Assert

        Assert.False(result);
    }

    [Fact]
    public async Task TransactionContext_WhenTransactionMatchesBudgetAndCategory_ReturnsExpectedPredicates()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);

        budget.AssociateCategory(category.Id, ownerId);

        var transaction = budget.AddTransaction(
            category.Id,
            accountId,
            "Operation",
            TransactionType.Expense,
            10m,
            PaymentMethod.Cash,
            null,
            ownerId);

        var repository = Substitute.For<ITransactionRepository>();
        var cache = new EntityCacheContext();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .GetTrackedByIdAsync(
                transaction.Id,
                cancellationToken)
            .Returns(transaction);

        var context = new TransactionContext(
            repository,
            cache);

        // Act

        var isInBudget = await context.IsInBudgetAsync(
            transaction.Id,
            budget.Id,
            cancellationToken);

        var isInCategory = await context.IsInCategoryAsync(
            transaction.Id,
            category.Id,
            cancellationToken);

        var isInBoth = await context.IsInBudgetAndCategoryAsync(
            transaction.Id,
            budget.Id,
            category.Id,
            cancellationToken);

        // Assert

        Assert.True(isInBudget);
        Assert.True(isInCategory);
        Assert.True(isInBoth);
    }

    [Fact]
    public async Task TransactionContext_WhenOnlyBudgetMatches_ReturnsFalseForCombinedPredicate()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);

        budget.AssociateCategory(category.Id, ownerId);

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
        var cache = new EntityCacheContext();
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .GetTrackedByIdAsync(
                transaction.Id,
                cancellationToken)
            .Returns(transaction);

        var context = new TransactionContext(
            repository,
            cache);

        // Act

        var result = await context.IsInBudgetAndCategoryAsync(
            transaction.Id,
            budget.Id,
            Guid.NewGuid(),
            cancellationToken);

        // Assert

        Assert.False(result);
    }
}
