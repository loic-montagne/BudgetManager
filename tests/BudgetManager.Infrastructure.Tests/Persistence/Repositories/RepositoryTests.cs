using BudgetManager.Domain.Enums;
using BudgetManager.Domain.ValueObjects;
using BudgetManager.Infrastructure.Persistence.Repositories;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence.Repositories;

[Collection(SqlServerCollection.Name)]
public sealed class RepositoryTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task GenericRepository_CreateAsync_PersistsEntity()
    {
        // Arrange

        var repository =
            new BankRepository(
                Context);

        var bank =
            TestData.Bank();

        // Act

        await repository.CreateAsync(
            bank,
            TestContext.Current.CancellationToken);

        // Assert

        Context.ChangeTracker.Clear();

        var persisted =
            await Context.Banks.SingleAsync(
                x => x.Id == bank.Id,
                TestContext.Current.CancellationToken);

        Assert.Equal(
            bank.Name,
            persisted.Name);
    }

    [Fact]
    public async Task GenericRepository_DeleteAsync_RemovesEntity()
    {
        // Arrange

        var repository =
            new BankRepository(
                Context);

        var bank =
            TestData.Bank();

        await repository.CreateAsync(
            bank,
            TestContext.Current.CancellationToken);

        // Act

        await repository.DeleteAsync(
            bank,
            TestContext.Current.CancellationToken);

        // Assert

        Assert.False(
            await Context.Banks.AnyAsync(
                x => x.Id == bank.Id,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GenericRepository_UpdateAsync_WhenEntityIsDetached_Throws()
    {
        // Arrange

        var repository =
            new BankRepository(
                Context);

        var bank =
            TestData.Bank();

        // Act

        var action = () => repository.UpdateAsync(
            bank,
            TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(
            action);
    }

    [Fact]
    public async Task BankRepository_GetTrackedByIdAsync_ReturnsTrackedBank()
    {
        // Arrange

        var (bank, _) =
            await TestData.AddBankAndAccountAsync(
                Context);

        Context.ChangeTracker.Clear();

        var repository =
            new BankRepository(
                Context);

        // Act

        var result =
            await repository.GetTrackedByIdAsync(
                bank.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Unchanged,
            Context.Entry(result).State);
    }

    [Fact]
    public async Task BudgetCategoryRepository_GetTrackedByIdAsync_LoadsAssociatedBudgets()
    {
        // Arrange

        var (_, budget, category, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        Context.ChangeTracker.Clear();

        var repository =
            new BudgetCategoryRepository(
                Context);

        // Act

        var result =
            await repository.GetTrackedByIdAsync(
                category.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Contains(
            result.Budgets,
            x => x.Id == budget.Id);

        Assert.Equal(
            EntityState.Unchanged,
            Context.Entry(result).State);
    }

    [Fact]
    public async Task TransactionRepository_GetTrackedByIdAsync_ReturnsTrackedTransaction()
    {
        // Arrange

        var (_, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var transaction =
            budget.Transactions.Single();

        Context.ChangeTracker.Clear();

        var repository =
            new TransactionRepository(
                Context);

        // Act

        var result =
            await repository.GetTrackedByIdAsync(
                transaction.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Unchanged,
            Context.Entry(result).State);
    }

    [Fact]
    public async Task AccountRepository_GetTrackedByIdAsync_ReturnsTrackedAccount()
    {
        // Arrange

        var (_, account) =
            await TestData.AddBankAndAccountAsync(
                Context);

        Context.ChangeTracker.Clear();

        var repository =
            new AccountRepository(
                Context);

        // Act

        var result =
            await repository.GetTrackedByIdAsync(
                account.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Unchanged,
            Context.Entry(result).State);
    }

    [Fact]
    public async Task AccountRepository_IsNameUniqueAsync_IsCaseAndAccentInsensitive()
    {
        // Arrange

        var bank =
            TestData.Bank();

        Context.Add(bank);

        Context.Add(
            TestData.Account(
                bank.Id,
                "Épargne"));

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var repository =
            new AccountRepository(
                Context);

        // Act

        var result =
            await repository.IsNameUniqueAsync(
                "epargne",
                null,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.False(result);
    }

    [Fact]
    public async Task AccountRepository_IsNameUniqueAsync_WhenExistingEntityIsExcluded_ReturnsTrue()
    {
        // Arrange

        var (_, account) =
            await TestData.AddBankAndAccountAsync(
                Context);

        var repository =
            new AccountRepository(
                Context);

        // Act

        var result =
            await repository.IsNameUniqueAsync(
                account.Name,
                account.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.True(result);
    }

    [Fact]
    public async Task AccountRepository_IsIbanUniqueAsync_WhenIbanExists_ReturnsFalse()
    {
        // Arrange

        var (_, account) =
            await TestData.AddBankAndAccountAsync(
                Context);

        var repository =
            new AccountRepository(
                Context);

        // Act

        var result =
            await repository.IsIbanUniqueAsync(
                account.Iban,
                null,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.False(result);
    }

    [Fact]
    public async Task AccountRepository_IsUsedAsync_WhenAccountIsTransactionSource_ReturnsTrue()
    {
        // Arrange

        var (_, _, _, _, account) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var repository =
            new AccountRepository(
                Context);

        // Act

        var result =
            await repository.IsUsedAsync(
                account.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.True(result);
    }

    [Fact]
    public async Task AccountRepository_IsUsedAsync_WhenAccountIsNotUsed_ReturnsFalse()
    {
        // Arrange

        var (_, account) =
            await TestData.AddBankAndAccountAsync(
                Context);

        var repository =
            new AccountRepository(
                Context);

        // Act

        var result =
            await repository.IsUsedAsync(
                account.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.False(result);
    }

    [Fact]
    public async Task BankRepository_IsNameUniqueAsync_IsCaseInsensitive()
    {
        // Arrange

        Context.Add(
            TestData.Bank(
                "My Bank"));

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var repository =
            new BankRepository(
                Context);

        // Act

        var result =
            await repository.IsNameUniqueAsync(
                "MY BANK",
                null,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.False(result);
    }

    [Fact]
    public async Task BankRepository_IsBicUniqueAsync_WhenBicExists_ReturnsFalse()
    {
        // Arrange

        var bank =
            TestData.Bank();

        Context.Add(bank);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var repository =
            new BankRepository(
                Context);

        // Act

        var result =
            await repository.IsBicUniqueAsync(
                bank.Bic,
                null,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.False(result);
    }

    [Fact]
    public async Task BankRepository_IsUsedAsync_WhenBankHasAccount_ReturnsTrue()
    {
        // Arrange

        var (bank, _) =
            await TestData.AddBankAndAccountAsync(
                Context);

        var repository =
            new BankRepository(
                Context);

        // Act

        var result =
            await repository.IsUsedAsync(
                bank.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.True(result);
    }

    [Fact]
    public async Task BudgetCategoryRepository_IsNameUniqueAsync_IsCaseInsensitive()
    {
        // Arrange

        var category =
            TestData.Category(
                "Food");

        Context.Add(category);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var repository =
            new BudgetCategoryRepository(
                Context);

        // Act

        var result =
            await repository.IsNameUniqueAsync(
                "FOOD",
                null,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.False(result);
    }

    [Fact]
    public async Task BudgetCategoryRepository_IsUsedAsync_WhenCategoryHasTransaction_ReturnsTrue()
    {
        // Arrange

        var (_, budget, category, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var repository =
            new BudgetCategoryRepository(
                Context);

        // Act

        var globallyUsed =
            await repository.IsUsedAsync(
                category.Id,
                TestContext.Current.CancellationToken);

        var usedInBudget =
            await repository.IsUsedAsync(
                category.Id,
                budget.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.True(globallyUsed);
        Assert.True(usedInBudget);
    }

    [Fact]
    public async Task BudgetCategoryRepository_IsUsedAsync_WhenCategoryIsNotUsed_ReturnsFalse()
    {
        // Arrange

        var category =
            TestData.Category();

        Context.Add(category);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var repository =
            new BudgetCategoryRepository(
                Context);

        // Act

        var result =
            await repository.IsUsedAsync(
                category.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.False(result);
    }

    [Fact]
    public async Task BudgetRepository_GetTrackedByIdAsync_LoadsCompleteAggregate()
    {
        // Arrange

        var (_, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        Context.ChangeTracker.Clear();

        var repository =
            new BudgetRepository(
                Context);

        // Act

        var result =
            await repository.GetTrackedByIdAsync(
                budget.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);
        Assert.NotEmpty(result.Accesses);
        Assert.NotEmpty(result.Categories);
        Assert.NotEmpty(result.Transactions);
    }

    [Fact]
    public async Task BudgetRepository_UpdateAsync_WhenOnlyAggregateChildChanges_ForcesRootUpdate()
    {
        // Arrange

        var (_, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var otherUser =
            await TestData.AddUserAsync(
                Context,
                "other");

        Context.ChangeTracker.Clear();

        var repository =
            new BudgetRepository(
                Context);

        var tracked =
            await repository.GetTrackedByIdAsync(
                budget.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(tracked);

        var originalRowVersion =
            tracked.RowVersion.ToArray();

        TimeProvider.UtcNow =
            TimeProvider.UtcNow.AddMinutes(1);

        tracked.SetPermissions(
            otherUser.Id,
            Permission.View,
            tracked.Accesses.Single(x => x.IsOwner).UserId);

        // Act

        await repository.UpdateAsync(
            tracked,
            TestContext.Current.CancellationToken);

        // Assert

        Assert.False(
            originalRowVersion.SequenceEqual(
                tracked.RowVersion));

        Assert.Equal(
            TimeProvider.UtcNow,
            tracked.UpdatedOn);
    }

    [Fact]
    public async Task BudgetRepository_TransferOwnershipAsync_WhenTargetWasMember_PersistsOwnerPermissionSemantics()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var target =
            await TestData.AddUserAsync(
                Context,
                "member-owner-semantics");

        budget.GrantAccess(
            target.Id,
            Permission.View,
            owner.Id);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        Context.ChangeTracker.Clear();

        var repository =
            new BudgetRepository(
                Context);

        var trackedBudget =
            await repository.GetTrackedByIdAsync(
                budget.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(
            trackedBudget);

        trackedBudget.TransferOwnership(
            target.Id,
            owner.Id);

        // Act

        await repository.TransferOwnershipAsync(
            trackedBudget,
            owner.Id,
            TestContext.Current.CancellationToken);

        // Assert

        Context.ChangeTracker.Clear();

        var persisted =
            await repository.GetTrackedByIdAsync(
                budget.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(
            persisted);

        var newOwner =
            Assert.Single(persisted.Accesses, x => x.IsOwner);

        Assert.Equal(
            target.Id,
            newOwner.UserId);

        Assert.True(
            newOwner.HasPermission(
                Permission.View));

        Assert.True(
            newOwner.HasPermission(
                Permission.Edit));

        Assert.True(
            newOwner.HasPermission(
                Permission.Share));

        Assert.True(
            newOwner.HasPermission(
                Permission.Lock));

        var previousOwner =
            Assert.Single(persisted.Accesses, x => x.UserId == owner.Id);

        Assert.False(
            previousOwner.IsOwner);

        Assert.True(
            previousOwner.HasPermission(
                Permission.View));

        Assert.True(
            previousOwner.HasPermission(
                Permission.Edit));

        Assert.True(
            previousOwner.HasPermission(
                Permission.Share));

        Assert.True(
            previousOwner.HasPermission(
                Permission.Lock));
    }

    [Fact]
    public async Task BudgetRepository_DeleteAsync_WhenBudgetHasCompleteGraph_DeletesAggregate()
    {
        // Arrange

        var (_, budget, category, _, account) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var budgetId =
            budget.Id;

        var transactionIds =
            budget.Transactions
                .Select(x => x.Id)
                .ToArray();

        var repository =
            new BudgetRepository(
                Context);

        // Act

        await repository.DeleteAsync(
            budget,
            TestContext.Current.CancellationToken);

        // Assert

        Context.ChangeTracker.Clear();

        Assert.False(
            await Context
                .Set<BudgetManager.Domain.Entities.Budget>()
                .AnyAsync(
                    x => x.Id == budgetId,
                    TestContext.Current.CancellationToken));

        Assert.False(
            await Context
                .Set<BudgetManager.Domain.Entities.BudgetAccess>()
                .AnyAsync(
                    x => x.BudgetId == budgetId,
                    TestContext.Current.CancellationToken));

        Assert.False(
            await Context
                .Set<BudgetManager.Domain.Entities.Transaction>()
                .AnyAsync(
                    x => transactionIds.Contains(x.Id),
                    TestContext.Current.CancellationToken));

        Assert.True(
            await Context
                .Set<BudgetManager.Domain.Entities.BudgetCategory>()
                .AnyAsync(
                    x => x.Id == category.Id,
                    TestContext.Current.CancellationToken));

        Assert.True(
            await Context
                .Set<BudgetManager.Domain.Entities.Account>()
                .AnyAsync(
                    x => x.Id == account.Id,
                    TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BudgetRepository_IsNameUniqueAsync_WhenBudgetExists_ReturnsFalse()
    {
        // Arrange

        var owner =
            await TestData.AddUserAsync(
                Context,
                "owner");

        var budget =
            BudgetManager.Domain.Entities.Budget.Create(
                "Household",
                owner.Id);

        Context.Add(budget);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var repository =
            new BudgetRepository(
                Context);

        // Act

        var result =
            await repository.IsNameUniqueAsync(
                "HOUSEHOLD",
                null,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.False(result);
    }

    [Fact]
    public async Task TransactionRepository_IsNameUniqueAsync_UsesBudgetAndCategoryScope()
    {
        // Arrange

        var (_, budget, category, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var transaction =
            budget.Transactions.Single();

        var repository =
            new TransactionRepository(
                Context);

        // Act

        var sameScope =
            await repository.IsNameUniqueAsync(
                transaction.Name,
                budget.Id,
                category.Id,
                null,
                TestContext.Current.CancellationToken);

        var otherBudget =
            await repository.IsNameUniqueAsync(
                transaction.Name,
                Guid.NewGuid(),
                category.Id,
                null,
                TestContext.Current.CancellationToken);

        var excluded =
            await repository.IsNameUniqueAsync(
                transaction.Name,
                budget.Id,
                category.Id,
                transaction.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.False(sameScope);
        Assert.True(otherBudget);
        Assert.True(excluded);
    }
    [Fact]
    public async Task BankRepository_DeleteAsync_WhenBankIsUsedByAccount_ThrowsUpdateException()
    {
        // Arrange

        var (bank, _) =
            await TestData.AddBankAndAccountAsync(
                Context);

        var repository =
            new BankRepository(
                Context);

        // Act

        var exception =
            await Record.ExceptionAsync(
                () => repository.DeleteAsync(
                    bank,
                    TestContext.Current.CancellationToken));

        // Assert

        Assert.NotNull(
            exception);

        Assert.True(
            exception is InvalidOperationException
            or BudgetManager.Application.Exceptions.UpdateException);
    }

    [Fact]
    public async Task AccountRepository_DeleteAsync_WhenAccountIsUsedByTransaction_ThrowsUpdateException()
    {
        // Arrange

        var (_, _, _, _, account) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var repository =
            new AccountRepository(
                Context);

        // Act

        var exception =
            await Record.ExceptionAsync(
                () => repository.DeleteAsync(
                    account,
                    TestContext.Current.CancellationToken));

        // Assert

        Assert.NotNull(
            exception);

        Assert.True(
            exception is InvalidOperationException
            or BudgetManager.Application.Exceptions.UpdateException);
    }


    [Fact]
    public async Task GenericRepository_CreateAsync_WhenEntityIsNull_Throws()
    {
        // Arrange

        var repository =
            new BankRepository(
                Context);

        // Act

        var action = () => repository.CreateAsync(
            null!,
            TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<ArgumentNullException>(
            action);
    }

    [Fact]
    public async Task GenericRepository_UpdateAsync_WhenEntityIsNull_Throws()
    {
        // Arrange

        var repository =
            new BankRepository(
                Context);

        // Act

        var action = () => repository.UpdateAsync(
            null!,
            TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<ArgumentNullException>(
            action);
    }

    [Fact]
    public async Task GenericRepository_DeleteAsync_WhenEntityIsNull_Throws()
    {
        // Arrange

        var repository =
            new BankRepository(
                Context);

        // Act

        var action = () => repository.DeleteAsync(
            null!,
            TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<ArgumentNullException>(
            action);
    }

    [Fact]
    public async Task BudgetRepository_UpdateAsync_WhenBudgetIsDetached_Throws()
    {
        // Arrange

        var ownerId =
            Guid.NewGuid();

        var budget =
            BudgetManager.Domain.Entities.Budget.Create(
                "Detached",
                ownerId);

        var repository =
            new BudgetRepository(
                Context);

        // Act

        var action = () => repository.UpdateAsync(
            budget,
            TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(
            action);
    }


}
