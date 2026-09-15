using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Contexts;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Exceptions;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class ContextsTests
{
    [Fact]
    public async Task AccountContext_WhenEntityIsRequestedTwice_ReturnsCachedTrackedEntity()
    {
        // Arrange

        var accountRepository = Substitute.For<IAccountRepository>();
        var entityCache = new EntityCacheContext();

        var bankId = Guid.NewGuid();

        var account = Account.Create(
            "Main",
            bankId,
            Iban.Create("FR7630006000011234567890189"));

        var cancellationToken = TestContext.Current.CancellationToken;

        accountRepository
            .GetTrackedByIdAsync(
                account.Id,
                cancellationToken)
            .Returns(account);

        var accountContext = new AccountContext(
            accountRepository,
            entityCache);

        // Act

        var firstResult = await accountContext.GetAsync(
            account.Id,
            cancellationToken);

        var secondResult = await accountContext.GetAsync(
            account.Id,
            cancellationToken);

        // Assert

        Assert.Same(account, firstResult);
        Assert.Same(account, secondResult);

        await accountRepository
            .Received(1)
            .GetTrackedByIdAsync(
                account.Id,
                cancellationToken);
    }

    [Fact]
    public async Task AccountContext_WhenAccountStateChanges_ReturnsExpectedOpenAndClosedStatuses()
    {
        // Arrange

        var accountRepository = Substitute.For<IAccountRepository>();
        var entityCache = new EntityCacheContext();
        var bankId = Guid.NewGuid();

        var account = Account.Create(
            "Main",
            bankId,
            Iban.Create("FR7630006000011234567890189"));

        var cancellationToken = TestContext.Current.CancellationToken;

        accountRepository
            .GetTrackedByIdAsync(
                account.Id,
                cancellationToken)
            .Returns(account);

        var accountContext = new AccountContext(
            accountRepository,
            entityCache);

        // Act

        var isOpened = await accountContext.IsOpenedAsync(
            account.Id,
            cancellationToken);

        account.Close();

        var isClosed = await accountContext.IsClosedAsync(
            account.Id,
            cancellationToken);

        // Assert

        Assert.True(isOpened);
        Assert.True(isClosed);
    }

    [Fact]
    public async Task BankContext_WhenEntityDoesNotExist_GetRequiredAsyncThrowsNotFoundException()
    {
        // Arrange

        var bankRepository = Substitute.For<IBankRepository>();
        var entityCache = new EntityCacheContext();
        var bankId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        bankRepository
            .GetTrackedByIdAsync(
                bankId,
                cancellationToken)
            .Returns((Bank?)null);

        var bankContext = new BankContext(
            bankRepository,
            entityCache);

        // Act

        var action = () => bankContext.GetRequiredAsync(
            bankId,
            cancellationToken);

        // Assert

        await Assert.ThrowsAsync<NotFoundException<Bank>>(action);
    }

    [Fact]
    public async Task BudgetContext_WhenBudgetStateChanges_MapsDomainStateToApplicationStatuses()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create(
            "Budget",
            ownerId);

        var budgetRepository = Substitute.For<IBudgetRepository>();
        var entityCache = new EntityCacheContext();
        var currentUser = new TestCurrentUser(
            authenticated: true,
            userId: ownerId);

        var cancellationToken = TestContext.Current.CancellationToken;

        budgetRepository
            .GetTrackedByIdAsync(
                budget.Id,
                cancellationToken)
            .Returns(budget);

        var budgetContext = new BudgetContext(
            budgetRepository,
            entityCache,
            currentUser);

        // Act

        var editableStatus = await budgetContext.IsEditableAsync(
            budget.Id,
            cancellationToken);

        budget.Lock(ownerId);

        var lockedStatus = await budgetContext.IsEditableAsync(
            budget.Id,
            cancellationToken);

        var unlockableStatus = await budgetContext.IsUnlockableAsync(
            budget.Id,
            cancellationToken);

        // Assert

        Assert.Equal(
            BudgetEditableStatus.Editable,
            editableStatus);

        Assert.Equal(
            BudgetEditableStatus.Locked,
            lockedStatus);

        Assert.Equal(
            BudgetUnlockableStatus.Unlockable,
            unlockableStatus);
    }

    [Fact]
    public async Task BudgetContext_WhenBudgetDoesNotExist_ReturnsNotAuthorizedAndNoPermission()
    {
        // Arrange

        var budgetRepository = Substitute.For<IBudgetRepository>();
        var entityCache = new EntityCacheContext();
        var currentUser = new TestCurrentUser();
        var budgetId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetRepository
            .GetTrackedByIdAsync(
                budgetId,
                cancellationToken)
            .Returns((Budget?)null);

        var budgetContext = new BudgetContext(
            budgetRepository,
            entityCache,
            currentUser);

        // Act

        var editableStatus = await budgetContext.IsEditableAsync(
            budgetId,
            cancellationToken);

        var hasPermission = await budgetContext.HasCurrentUserPermissionAsync(
            budgetId,
            Permission.View,
            cancellationToken);

        // Assert

        Assert.Equal(
            BudgetEditableStatus.NotAuthorized,
            editableStatus);

        Assert.False(hasPermission);
    }

    [Fact]
    public async Task BudgetContext_WhenCheckingOwnership_ReturnsExpectedResults()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        budget.GrantAccess(
            memberId,
            Permission.View,
            ownerId);

        var budgetRepository =
            Substitute.For<IBudgetRepository>();

        var entityCache = new EntityCacheContext();

        var currentUser = new TestCurrentUser(
            authenticated: true,
            userId: ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        budgetRepository
            .GetTrackedByIdAsync(
                budget.Id,
                cancellationToken)
            .Returns(budget);

        var budgetContext = new BudgetContext(
            budgetRepository,
            entityCache,
            currentUser);

        // Act

        var ownerIsOwner =
            await budgetContext.IsOwnerAsync(
                budget.Id,
                ownerId,
                cancellationToken);

        var memberIsOwner =
            await budgetContext.IsOwnerAsync(
                budget.Id,
                memberId,
                cancellationToken);

        var ownerIsNotOwner =
            await budgetContext.IsNotOwnerAsync(
                budget.Id,
                ownerId,
                cancellationToken);

        var memberIsNotOwner =
            await budgetContext.IsNotOwnerAsync(
                budget.Id,
                memberId,
                cancellationToken);

        // Assert

        Assert.True(ownerIsOwner);
        Assert.False(memberIsOwner);

        Assert.False(ownerIsNotOwner);
        Assert.True(memberIsNotOwner);
    }

    [Fact]
    public async Task BudgetContext_WhenBudgetCanBeLocked_ReturnsLockable()
    {
        // Arrange

        var ownerId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        var budgetRepository =
            Substitute.For<IBudgetRepository>();

        var entityCache =
            new EntityCacheContext();

        var currentUser =
            new TestCurrentUser(
                authenticated: true,
                userId: ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        budgetRepository
            .GetTrackedByIdAsync(
                budget.Id,
                cancellationToken)
            .Returns(budget);

        var budgetContext =
            new BudgetContext(
                budgetRepository,
                entityCache,
                currentUser);

        // Act

        var status =
            await budgetContext.IsLockableAsync(
                budget.Id,
                cancellationToken);

        // Assert

        Assert.Equal(
            BudgetLockableStatus.Lockable,
            status);
    }

    [Fact]
    public async Task BudgetContext_WhenBudgetIsAlreadyLocked_ReturnsAlreadyLocked()
    {
        // Arrange

        var ownerId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        budget.Lock(ownerId);

        var budgetRepository =
            Substitute.For<IBudgetRepository>();

        var entityCache =
            new EntityCacheContext();

        var currentUser =
            new TestCurrentUser(
                authenticated: true,
                userId: ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        budgetRepository
            .GetTrackedByIdAsync(
                budget.Id,
                cancellationToken)
            .Returns(budget);

        var budgetContext =
            new BudgetContext(
                budgetRepository,
                entityCache,
                currentUser);

        // Act

        var status =
            await budgetContext.IsLockableAsync(
                budget.Id,
                cancellationToken);

        // Assert

        Assert.Equal(
            BudgetLockableStatus.AlreadyLocked,
            status);
    }

    [Fact]
    public async Task BudgetContext_WhenCurrentUserCannotLockBudget_ReturnsNotAuthorized()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        var budgetRepository =
            Substitute.For<IBudgetRepository>();

        var entityCache =
            new EntityCacheContext();

        var currentUser =
            new TestCurrentUser(
                authenticated: true,
                userId: unauthorizedUserId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        budgetRepository
            .GetTrackedByIdAsync(
                budget.Id,
                cancellationToken)
            .Returns(budget);

        var budgetContext =
            new BudgetContext(
                budgetRepository,
                entityCache,
                currentUser);

        // Act

        var status =
            await budgetContext.IsLockableAsync(
                budget.Id,
                cancellationToken);

        // Assert

        Assert.Equal(
            BudgetLockableStatus.NotAuthorized,
            status);
    }

    [Fact]
    public async Task BudgetContext_WhenBudgetCanBeUnlocked_ReturnsUnlockable()
    {
        // Arrange

        var ownerId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        budget.Lock(ownerId);

        var budgetRepository =
            Substitute.For<IBudgetRepository>();

        var entityCache =
            new EntityCacheContext();

        var currentUser =
            new TestCurrentUser(
                authenticated: true,
                userId: ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        budgetRepository
            .GetTrackedByIdAsync(
                budget.Id,
                cancellationToken)
            .Returns(budget);

        var budgetContext =
            new BudgetContext(
                budgetRepository,
                entityCache,
                currentUser);

        // Act

        var status =
            await budgetContext.IsUnlockableAsync(
                budget.Id,
                cancellationToken);

        // Assert

        Assert.Equal(
            BudgetUnlockableStatus.Unlockable,
            status);
    }

    [Fact]
    public async Task BudgetContext_WhenBudgetIsAlreadyUnlocked_ReturnsAlreadyUnlocked()
    {
        // Arrange

        var ownerId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        var budgetRepository =
            Substitute.For<IBudgetRepository>();

        var entityCache =
            new EntityCacheContext();

        var currentUser =
            new TestCurrentUser(
                authenticated: true,
                userId: ownerId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        budgetRepository
            .GetTrackedByIdAsync(
                budget.Id,
                cancellationToken)
            .Returns(budget);

        var budgetContext =
            new BudgetContext(
                budgetRepository,
                entityCache,
                currentUser);

        // Act

        var status =
            await budgetContext.IsUnlockableAsync(
                budget.Id,
                cancellationToken);

        // Assert

        Assert.Equal(
            BudgetUnlockableStatus.AlreadyUnlocked,
            status);
    }

    [Fact]
    public async Task BudgetContext_WhenCurrentUserCannotUnlockBudget_ReturnsNotAuthorized()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        budget.Lock(ownerId);

        var budgetRepository =
            Substitute.For<IBudgetRepository>();

        var entityCache =
            new EntityCacheContext();

        var currentUser =
            new TestCurrentUser(
                authenticated: true,
                userId: unauthorizedUserId);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        budgetRepository
            .GetTrackedByIdAsync(
                budget.Id,
                cancellationToken)
            .Returns(budget);

        var budgetContext =
            new BudgetContext(
                budgetRepository,
                entityCache,
                currentUser);

        // Act

        var status =
            await budgetContext.IsUnlockableAsync(
                budget.Id,
                cancellationToken);

        // Assert

        Assert.Equal(
            BudgetUnlockableStatus.NotAuthorized,
            status);
    }

    [Fact]
    public void EntityCacheContext_WhenEntitiesHaveSameId_KeepsTypesSeparated()
    {
        // Arrange

        var id = Guid.NewGuid();

        var bank = Bank.Create(
            "Banque",
            Bic.Create("BNPAFRPP"));

        var category = BudgetCategory.Create(
            "Catégorie",
            null);

        var cache = new EntityCacheContext();

        // Act

        cache.Set(id, bank);
        cache.Set(id, category);

        var bankWasFound =
            cache.TryGet<Bank>(
                id,
                out var cachedBank);

        var categoryWasFound =
            cache.TryGet<BudgetCategory>(
                id,
                out var cachedCategory);

        // Assert

        Assert.True(bankWasFound);
        Assert.True(categoryWasFound);

        Assert.Same(bank, cachedBank);
        Assert.Same(category, cachedCategory);
    }

    [Fact]
    public async Task TransactionContext_WhenTransactionDoesNotExist_ReturnsFalseForRelationshipPredicates()
    {
        // Arrange

        var transactionRepository = Substitute.For<ITransactionRepository>();
        var entityCache = new EntityCacheContext();
        var transactionId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        transactionRepository
            .GetTrackedByIdAsync(
                transactionId,
                cancellationToken)
            .Returns((Transaction?)null);

        var transactionContext = new TransactionContext(
            transactionRepository,
            entityCache);

        // Act

        var isInBudget = await transactionContext.IsInBudgetAsync(
            transactionId,
            budgetId,
            cancellationToken);

        var isInCategory = await transactionContext.IsInCategoryAsync(
            transactionId,
            categoryId,
            cancellationToken);

        // Assert

        Assert.False(isInBudget);
        Assert.False(isInCategory);
    }
}
