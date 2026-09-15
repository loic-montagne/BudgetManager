using BudgetManager.Application.Exceptions;
using BudgetManager.Domain.Entities;
using BudgetManager.Infrastructure.Persistence.Repositories;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence;

[Collection(SqlServerCollection.Name)]
public sealed class ApplicationDbContextTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task SaveChangesAsync_WhenEntityIsAdded_AuditsEntity()
    {
        // Arrange

        var bank = TestData.Bank();

        // Act

        Context.Add(bank);
        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            CurrentUser.UserId,
            bank.CreatedBy);

        Assert.Equal(
            TimeProvider.UtcNow,
            bank.CreatedOn);

        Assert.Equal(
            CurrentUser.UserId,
            bank.UpdatedBy);

        Assert.Equal(
            TimeProvider.UtcNow,
            bank.UpdatedOn);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityIsModified_UpdatesAuditInformation()
    {
        // Arrange

        var bank = TestData.Bank();
        Context.Add(bank);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var updatedBy = Guid.NewGuid();

        CurrentUser.UserId = updatedBy;
        TimeProvider.UtcNow =
            TimeProvider.UtcNow.AddMinutes(5);

        bank.Rename(
            "Updated bank");

        // Act

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            updatedBy,
            bank.UpdatedBy);

        Assert.Equal(
            TimeProvider.UtcNow,
            bank.UpdatedOn);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenUniqueConstraintIsViolated_ThrowsUpdateException()
    {
        // Arrange

        Context.Add(
            TestData.Bank(
                "Unique Bank",
                "BNPAFRPP"));

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        Context.Add(
            TestData.Bank(
                "Unique Bank",
                "AGRIFRPP"));

        // Act

        var action = () => Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<UpdateException>(
            action);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenIbanUniqueIndexIsViolated_ThrowsUpdateException()
    {
        // Arrange

        var bank = TestData.Bank();
        Context.Add(bank);

        var first = TestData.Account(
            bank.Id,
            "First",
            1);

        var second = TestData.Account(
            bank.Id,
            "Second",
            1);

        Context.AddRange(
            first,
            second);

        // Act

        var action = () => Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<UpdateException>(
            action);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenBicUniqueIndexIsViolated_ThrowsUpdateException()
    {
        // Arrange

        Context.AddRange(
            TestData.Bank(
                "First",
                "BNPAFRPP"),
            TestData.Bank(
                "Second",
                "BNPAFRPP"));

        // Act

        var action = () => Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<UpdateException>(
            action);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenRowVersionIsStale_ThrowsConcurrencyException()
    {
        // Arrange

        var bank = TestData.Bank();

        Context.Add(bank);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        await using var firstContext =
            CreateContext();

        await using var secondContext =
            CreateContext();

        var first =
            await firstContext.Banks.SingleAsync(
                x => x.Id == bank.Id,
                TestContext.Current.CancellationToken);

        var second =
            await secondContext.Banks.SingleAsync(
                x => x.Id == bank.Id,
                TestContext.Current.CancellationToken);

        first.Rename(
            "First update");

        await firstContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        second.Rename(
            "Second update");

        // Act

        var action = () => secondContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<ConcurrencyException>(
            action);
    }

    [Fact]
    public async Task Migration_WhenApplied_CreatesExpectedManualUniqueIndexes()
    {
        // Arrange

        const string sql = """
            SELECT COUNT(*) AS Value
            FROM sys.indexes
            WHERE name IN ('IX_Accounts_Iban', 'IX_Banks_Bic')
              AND is_unique = 1
            """;

        // Act

        var count = await Context.Database
            .SqlQueryRaw<int>(sql)
            .SingleAsync(
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            2,
            count);
    }
    [Fact]
    public async Task SaveChangesAsync_WhenNoCurrentUser_AuditsWithEmptyUserId()
    {
        // Arrange

        await using var context =
            CreateContext(
                new TestCurrentUser());

        var bank =
            TestData.Bank();

        // Act

        context.Add(bank);

        await context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            Guid.Empty,
            bank.CreatedBy);

        Assert.Equal(
            Guid.Empty,
            bank.UpdatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenUpdatingWithoutCurrentUser_PreservesUpdatedBy()
    {
        // Arrange

        var bank =
            TestData.Bank();

        Context.Add(bank);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var originalUpdatedBy =
            bank.UpdatedBy;

        var anonymousUser =
            new TestCurrentUser();

        var laterTime =
            new MutableTimeProvider(
                TimeProvider.UtcNow.AddMinutes(10));

        await using var context =
            CreateContext(
                anonymousUser,
                laterTime);

        var tracked =
            await context.Banks.SingleAsync(
                x => x.Id == bank.Id,
                TestContext.Current.CancellationToken);

        tracked.Rename(
            "Updated anonymously");

        // Act

        await context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            originalUpdatedBy,
            tracked.UpdatedBy);

        Assert.Equal(
            laterTime.UtcNow,
            tracked.UpdatedOn);
    }

    [Fact]
    public void SaveChanges_WhenUniqueConstraintIsViolated_ThrowsUpdateException()
    {
        // Arrange

        Context.Add(
            TestData.Bank(
                "Unique Sync",
                "BNPAFRPP"));

        Context.SaveChanges();

        Context.Add(
            TestData.Bank(
                "Unique Sync",
                "AGRIFRPP"));

        // Act

        Action action = () =>
            Context.SaveChanges();

        // Assert

        Assert.Throws<UpdateException>(
            action);
    }

    [Fact]
    public async Task SaveChangesAsyncWithAcceptAllChanges_WhenUniqueConstraintIsViolated_ThrowsUpdateException()
    {
        // Arrange

        Context.Add(
            TestData.Bank(
                "Unique Async Bool",
                "BNPAFRPP"));

        await Context.SaveChangesAsync(
            true,
            TestContext.Current.CancellationToken);

        Context.Add(
            TestData.Bank(
                "Unique Async Bool",
                "AGRIFRPP"));

        // Act

        var action = () => Context.SaveChangesAsync(
            true,
            TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<UpdateException>(
            action);
    }

    [Fact]
    public void SaveChangesWithAcceptAllChanges_WhenUniqueConstraintIsViolated_ThrowsUpdateException()
    {
        // Arrange

        Context.Add(
            TestData.Bank(
                "Unique Sync Bool",
                "BNPAFRPP"));

        Context.SaveChanges(
            true);

        Context.Add(
            TestData.Bank(
                "Unique Sync Bool",
                "AGRIFRPP"));

        // Act

        Action action = () =>
            Context.SaveChanges(
                true);

        // Assert

        Assert.Throws<UpdateException>(
            action);
    }


    [Fact]
    public async Task Database_WhenSecondOwnerIsInsertedForSameBudget_RejectsWrite()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var secondOwner =
            await TestData.AddUserAsync(
                Context,
                "second-owner");

        var now =
            TimeProvider.UtcNow;

        // Act

        var action = () =>
            Context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO BudgetAccesses
                    (UserId, BudgetId, IsOwner, Permissions, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
                VALUES
                    ({secondOwner.Id}, {budget.Id}, {true}, {0}, {owner.Id}, {now}, {owner.Id}, {now})
                """,
                TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<SqlException>(
            action);
    }


    [Fact]
    public async Task Database_WhenDeletingBudgetOwnerWithExistingAccess_RejectsWrite()
    {
        // Arrange

        var owner =
            await TestData.AddUserAsync(
                Context,
                "owner-delete");

        var budget =
            BudgetManager.Domain.Entities.Budget.Create(
                "Protected budget",
                owner.Id);

        Context.Add(
            budget);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        // Act

        var exception =
            Record.Exception(
                () => Context.Remove(
                    owner));

        // Assert

        Assert.NotNull(
            exception);

        Assert.IsType<InvalidOperationException>(
            exception);

        Context.ChangeTracker.Clear();

        Assert.True(
            await Context
                .Set<BudgetManager.Infrastructure.Identity.ApplicationUser>()
                .AnyAsync(
                    x => x.Id == owner.Id,
                    TestContext.Current.CancellationToken));

        Assert.True(
            await Context
                .Set<BudgetManager.Domain.Entities.BudgetAccess>()
                .AnyAsync(
                    x => x.UserId == owner.Id &&
                         x.BudgetId == budget.Id,
                    TestContext.Current.CancellationToken));
    }

}
