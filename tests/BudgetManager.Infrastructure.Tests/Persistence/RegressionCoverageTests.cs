using BudgetManager.Application.Contexts;
using BudgetManager.Application.Exceptions;
using BudgetManager.Domain.Enums;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Infrastructure.Persistence.Queries;
using BudgetManager.Infrastructure.Persistence.Repositories;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence;

[Collection(SqlServerCollection.Name)]
public sealed class RegressionCoverageTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task AccountSearch_WhenSearchContainsOnlyWhitespace_DoesNotFilterResults()
    {
        // Arrange

        var bank =
            TestData.Bank();

        Context.Add(bank);

        Context.AddRange(
            TestData.Account(
                bank.Id,
                "First",
                1),
            TestData.Account(
                bank.Id,
                "Second",
                2));

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                null,
                true,
                [],
                "   ",
                null,
                null,
                null);

        var queries =
            new AccountQueries(
                Context,
                NullLogger<AccountQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            2,
            result.Results.Count);

        Assert.Equal(
            2,
            result.FilteredCount);
    }

    [Fact]
    public async Task UserQueries_GetCompleteByIdAsync_WhenUserHasNoRole_ReturnsUserWithEmptyRoles()
    {
        // Arrange

        var user =
            await TestData.AddUserAsync(
                Context,
                "no-role");

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.GetCompleteByIdAsync(
                user.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            user.Id,
            result.Id);

        Assert.Empty(
            result.Roles);
    }

    [Fact]
    public async Task AccountRepository_IsUsedAsync_WhenAccountIsTransferTarget_ReturnsTrue()
    {
        // Arrange

        var (owner, budget, category, bank, sourceAccount) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var transferAccount =
            TestData.Account(
                bank.Id,
                "Transfer target",
                2);

        Context.Add(
            transferAccount);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        budget.AddTransaction(
            category.Id,
            sourceAccount.Id,
            "Transfer",
            TransactionType.Expense,
            10m,
            PaymentMethod.BankTransfer,
            transferAccount.Id,
            owner.Id);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var repository =
            new AccountRepository(
                Context);

        // Act

        var result =
            await repository.IsUsedAsync(
                transferAccount.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.True(
            result);
    }

    [Theory]
    [InlineData("Bank")]
    [InlineData("Account")]
    [InlineData("BudgetCategory")]
    [InlineData("Transaction")]
    public async Task TrackedRepository_GetTrackedByIdAsync_WhenEntityDoesNotExist_ReturnsNull(
        string repositoryName)
    {
        // Arrange

        var id =
            Guid.NewGuid();

        // Act / Assert

        switch (repositoryName)
        {
            case "Bank":
                Assert.Null(
                    await new BankRepository(Context)
                        .GetTrackedByIdAsync(
                            id,
                            TestContext.Current.CancellationToken));
                break;

            case "Account":
                Assert.Null(
                    await new AccountRepository(Context)
                        .GetTrackedByIdAsync(
                            id,
                            TestContext.Current.CancellationToken));
                break;

            case "BudgetCategory":
                Assert.Null(
                    await new BudgetCategoryRepository(Context)
                        .GetTrackedByIdAsync(
                            id,
                            TestContext.Current.CancellationToken));
                break;

            case "Transaction":
                Assert.Null(
                    await new TransactionRepository(Context)
                        .GetTrackedByIdAsync(
                            id,
                            TestContext.Current.CancellationToken));
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown repository {repositoryName}.");
        }
    }

    [Fact]
    public async Task RepositoryUniquenessChecks_WhenExistingEntityIsExcluded_ReturnTrue()
    {
        // Arrange

        var (owner, budget, category, bank, account) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var transaction =
            budget.Transactions.Single();

        // Act

        var bankName =
            await new BankRepository(Context)
                .IsNameUniqueAsync(
                    bank.Name,
                    bank.Id,
                    TestContext.Current.CancellationToken);

        var bankBic =
            await new BankRepository(Context)
                .IsBicUniqueAsync(
                    bank.Bic,
                    bank.Id,
                    TestContext.Current.CancellationToken);

        var accountIban =
            await new AccountRepository(Context)
                .IsIbanUniqueAsync(
                    account.Iban,
                    account.Id,
                    TestContext.Current.CancellationToken);

        var categoryName =
            await new BudgetCategoryRepository(Context)
                .IsNameUniqueAsync(
                    category.Name,
                    category.Id,
                    TestContext.Current.CancellationToken);

        var budgetName =
            await new BudgetRepository(Context)
                .IsNameUniqueAsync(
                    budget.Name,
                    budget.Id,
                    TestContext.Current.CancellationToken);

        var transactionName =
            await new TransactionRepository(Context)
                .IsNameUniqueAsync(
                    transaction.Name,
                    budget.Id,
                    category.Id,
                    transaction.Id,
                    TestContext.Current.CancellationToken);

        // Assert

        Assert.True(bankName);
        Assert.True(bankBic);
        Assert.True(accountIban);
        Assert.True(categoryName);
        Assert.True(budgetName);
        Assert.True(transactionName);
    }

    [Fact]
    public async Task BudgetRepository_TransferOwnershipAsync_WhenTargetIsExistingMember_PersistsSingleOwner()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var target =
            await TestData.AddUserAsync(
                Context,
                "existing-member");

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

        // Act

        trackedBudget.TransferOwnership(
            target.Id,
            owner.Id);

        await repository.TransferOwnershipAsync(
            trackedBudget,
            owner.Id,
            TestContext.Current.CancellationToken);

        // Assert

        Context.ChangeTracker.Clear();

        var accesses =
            await Context
                .Set<BudgetManager.Domain.Entities.BudgetAccess>()
                .Where(x => x.BudgetId == budget.Id)
                .ToListAsync(
                    TestContext.Current.CancellationToken);

        var persistedOwner =
            Assert.Single(accesses, x => x.IsOwner);

        Assert.Equal(
            target.Id,
            persistedOwner.UserId);

        var oldOwner =
            Assert.Single(accesses, x => x.UserId == owner.Id);

        Assert.False(
            oldOwner.IsOwner);
    }

    [Fact]
    public async Task BudgetRepository_TransferOwnershipAsync_WhenTargetIsNewMember_PersistsSingleOwner()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var target =
            await TestData.AddUserAsync(
                Context,
                "new-member");

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

        // Act

        trackedBudget.TransferOwnership(
            target.Id,
            owner.Id);

        await repository.TransferOwnershipAsync(
            trackedBudget,
            owner.Id,
            TestContext.Current.CancellationToken);

        // Assert

        Context.ChangeTracker.Clear();

        var accesses =
            await Context
                .Set<BudgetManager.Domain.Entities.BudgetAccess>()
                .Where(x => x.BudgetId == budget.Id)
                .ToListAsync(
                    TestContext.Current.CancellationToken);

        var persistedOwner =
            Assert.Single(accesses, x => x.IsOwner);

        Assert.Equal(
            target.Id,
            persistedOwner.UserId);

        Assert.Contains(
            accesses,
            x => x.UserId == owner.Id &&
                 !x.IsOwner);
    }

    [Fact]
    public async Task BudgetRepository_UpdateAsync_WhenBudgetIsRenamed_PersistsRenameWithoutOwnershipTransfer()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

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

        trackedBudget.Rename(
            "Renamed budget",
            owner.Id);

        // Act

        await repository.UpdateAsync(
            trackedBudget,
            TestContext.Current.CancellationToken);

        // Assert

        Context.ChangeTracker.Clear();

        var persisted =
            await Context
                .Set<BudgetManager.Domain.Entities.Budget>()
                .SingleAsync(
                    x => x.Id == budget.Id,
                    TestContext.Current.CancellationToken);

        Assert.Equal(
            "Renamed budget",
            persisted.Name);

        var owners =
            await Context
                .Set<BudgetManager.Domain.Entities.BudgetAccess>()
                .CountAsync(
                    x => x.BudgetId == budget.Id &&
                         x.IsOwner,
                    TestContext.Current.CancellationToken);

        Assert.Equal(
            1,
            owners);
    }

    [Fact]
    public async Task BudgetRepository_TransferOwnershipAsync_WhenBudgetIsDetached_Throws()
    {
        // Arrange

        var ownerId =
            Guid.NewGuid();

        var budget =
            BudgetManager.Domain.Entities.Budget.Create(
                "Detached budget",
                ownerId);

        var repository =
            new BudgetRepository(
                Context);

        // Act

        var action =
            () => repository.TransferOwnershipAsync(
                budget,
                ownerId,
                TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(
            action);
    }

    [Fact]
    public async Task BudgetRepository_TransferOwnershipAsync_WhenBudgetIsNull_Throws()
    {
        // Arrange

        var repository =
            new BudgetRepository(
                Context);

        // Act

        var action =
            () => repository.TransferOwnershipAsync(
                null!,
                Guid.NewGuid(),
                TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<ArgumentNullException>(
            action);
    }

    [Fact]
    public async Task BudgetRepository_TransferOwnershipAsync_WhenPreviousOwnerIdIsWrong_RollsBackAndKeepsOriginalOwner()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var target =
            await TestData.AddUserAsync(
                Context,
                "wrong-previous-owner-target");

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

        var action =
            () => repository.TransferOwnershipAsync(
                trackedBudget,
                Guid.NewGuid(),
                TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(
            action);

        Context.ChangeTracker.Clear();

        var accesses =
            await Context
                .Set<BudgetManager.Domain.Entities.BudgetAccess>()
                .Where(x => x.BudgetId == budget.Id)
                .ToListAsync(
                    TestContext.Current.CancellationToken);

        var persistedOwner =
            Assert.Single(accesses, x => x.IsOwner);

        Assert.Equal(
            owner.Id,
            persistedOwner.UserId);

        Assert.DoesNotContain(
            accesses,
            x => x.UserId == target.Id &&
                 x.IsOwner);
    }

    [Fact]
    public async Task BudgetRepository_TransferOwnershipAsync_WhenSaveFails_RollsBackPreviousOwner()
    {
        // Arrange

        var (owner, budget, _, bank, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var target =
            await TestData.AddUserAsync(
                Context,
                "rollback-target");

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

        Context.Add(
            TestData.Bank(
                bank.Name,
                bank.Bic.Value));

        // Act

        var action =
            () => repository.TransferOwnershipAsync(
                trackedBudget,
                owner.Id,
                TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<UpdateException>(
            action);

        Context.ChangeTracker.Clear();

        var accesses =
            await Context
                .Set<BudgetManager.Domain.Entities.BudgetAccess>()
                .Where(x => x.BudgetId == budget.Id)
                .ToListAsync(
                    TestContext.Current.CancellationToken);

        var persistedOwner =
            Assert.Single(accesses, x => x.IsOwner);

        Assert.Equal(
            owner.Id,
            persistedOwner.UserId);

        Assert.DoesNotContain(
            accesses,
            x => x.UserId == target.Id &&
                 x.IsOwner);
    }

    [Fact]
    public async Task Database_WhenDeletingUntrackedUserReferencedByBudgetAccess_ThrowsUpdateException()
    {
        // Arrange

        var owner =
            await TestData.AddUserAsync(
                Context,
                "db-restrict-owner");

        var budget =
            BudgetManager.Domain.Entities.Budget.Create(
                "DB restrict budget",
                owner.Id);

        Context.Add(
            budget);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var ownerId =
            owner.Id;

        Context.ChangeTracker.Clear();

        var reloadedOwner =
            await Context
                .Set<ApplicationUser>()
                .SingleAsync(
                    x => x.Id == ownerId,
                    TestContext.Current.CancellationToken);

        Context.Remove(
            reloadedOwner);

        // Act

        var action =
            () => Context.SaveChangesAsync(
                TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<UpdateException>(
            action);
    }

    [Fact]
    public async Task Database_WhenDeletingBudgetWithoutTrackedDependents_CascadesAggregateChildren()
    {
        // Arrange

        var (_, budget, category, _, account) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var budgetId =
            budget.Id;

        var transactionId =
            budget.Transactions.Single().Id;

        Context.ChangeTracker.Clear();

        var reloadedBudget =
            await Context
                .Set<BudgetManager.Domain.Entities.Budget>()
                .SingleAsync(
                    x => x.Id == budgetId,
                    TestContext.Current.CancellationToken);

        Context.Remove(
            reloadedBudget);

        // Act

        await Context.SaveChangesAsync(
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
                    x => x.Id == transactionId,
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
    public async Task BudgetRepository_TransferOwnershipAsync_WhenOwnershipWasChangedByAnotherContext_RejectsStaleTransfer()
    {
        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(Context, CurrentUser);

        var firstTarget =
            await TestData.AddUserAsync(Context, "first-concurrent-owner");

        var secondTarget =
            await TestData.AddUserAsync(Context, "second-concurrent-owner");

        Context.ChangeTracker.Clear();

        await using var staleContext = CreateContext();

        var firstRepository = new BudgetRepository(Context);
        var staleRepository = new BudgetRepository(staleContext);

        var firstBudget =
            await firstRepository.GetTrackedByIdAsync(
                budget.Id,
                TestContext.Current.CancellationToken);

        var staleBudget =
            await staleRepository.GetTrackedByIdAsync(
                budget.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(firstBudget);
        Assert.NotNull(staleBudget);

        firstBudget.TransferOwnership(firstTarget.Id, owner.Id);

        await firstRepository.TransferOwnershipAsync(
            firstBudget,
            owner.Id,
            TestContext.Current.CancellationToken);

        staleBudget.TransferOwnership(secondTarget.Id, owner.Id);

        var action =
            () => staleRepository.TransferOwnershipAsync(
                staleBudget,
                owner.Id,
                TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(action);

        Context.ChangeTracker.Clear();

        var accesses =
            await Context
                .Set<BudgetManager.Domain.Entities.BudgetAccess>()
                .Where(x => x.BudgetId == budget.Id)
                .ToListAsync(TestContext.Current.CancellationToken);

        var persistedOwner =
            Assert.Single(accesses, x => x.IsOwner);

        Assert.Equal(firstTarget.Id, persistedOwner.UserId);

        Assert.DoesNotContain(
            accesses,
            x => x.UserId == secondTarget.Id && x.IsOwner);
    }

    [Fact]
    public async Task BudgetCategoryContext_WhenCategoryIsAssociated_ReturnsTrue()
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

        var applicationContext =
            new BudgetCategoryContext(
                repository,
                new EntityCacheContext());

        // Act

        var result =
            await applicationContext.IsAssociatedToBudgetAsync(
                category.Id,
                budget.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.True(
            result);
    }
}
