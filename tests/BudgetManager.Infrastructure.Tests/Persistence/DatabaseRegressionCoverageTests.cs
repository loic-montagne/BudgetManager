using BudgetManager.Application.Exceptions;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Infrastructure.Persistence.Queries;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence;

[Collection(SqlServerCollection.Name)]
public sealed class DatabaseRegressionCoverageTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task Database_WhenDeletingUntrackedBankReferencedByAccount_RejectsDelete()
    {
        var (bank, _) = await TestData.AddBankAndAccountAsync(Context);
        Context.ChangeTracker.Clear();

        var persisted = await Context.Set<Bank>().SingleAsync(
            x => x.Id == bank.Id,
            TestContext.Current.CancellationToken);

        Context.Remove(persisted);

        var action = () => Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
    }

    [Fact]
    public async Task Database_WhenDeletingUntrackedSourceAccountReferencedByTransaction_RejectsDelete()
    {
        var (_, _, _, _, account) = await TestData.AddBudgetGraphAsync(Context, CurrentUser);
        Context.ChangeTracker.Clear();

        var persisted = await Context.Set<Account>().SingleAsync(
            x => x.Id == account.Id,
            TestContext.Current.CancellationToken);

        Context.Remove(persisted);

        var action = () => Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
    }

    [Fact]
    public async Task Database_WhenDeletingUntrackedTransferAccountReferencedByTransaction_RejectsDelete()
    {
        var (owner, budget, category, bank, account) = await TestData.AddBudgetGraphAsync(Context, CurrentUser);
        var transferAccount = TestData.Account(
            bank.Id,
            "Transfer target",
            2);

        Context.Add(transferAccount);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        budget.AddTransaction(
            category.Id,
            account.Id,
            "Transfer",
            TransactionType.Expense,
            25m,
            PaymentMethod.BankTransfer,
            transferAccount.Id,
            owner.Id);

        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        var persisted = await Context.Set<Account>().SingleAsync(
            x => x.Id == transferAccount.Id,
            TestContext.Current.CancellationToken);

        Context.Remove(persisted);

        var action = () => Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
    }

    [Fact]
    public async Task Database_WhenDeletingUntrackedCategoryReferencedByTransaction_RejectsDelete()
    {
        var (_, _, category, _, _) = await TestData.AddBudgetGraphAsync(Context, CurrentUser);
        Context.ChangeTracker.Clear();

        var persisted = await Context.Set<BudgetCategory>().SingleAsync(
            x => x.Id == category.Id,
            TestContext.Current.CancellationToken);

        Context.Remove(persisted);

        var action = () => Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
    }

    [Fact]
    public async Task Database_WhenBankNamesDifferOnlyByAccent_RejectsSecondInsert()
    {
        Context.Add(TestData.Bank("Café Banque", "BNPAFRPP"));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Context.Add(TestData.Bank("Cafe Banque", "SOGEFRPP"));

        var action = () => Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
    }

    [Fact]
    public async Task Database_WhenBankBicIsDuplicated_RejectsSecondInsert()
    {
        Context.Add(TestData.Bank("First bank", "BNPAFRPP"));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Context.Add(TestData.Bank("Second bank", "BNPAFRPP"));

        var action = () => Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
    }

    [Fact]
    public async Task Database_WhenAccountIbanIsDuplicated_RejectsSecondInsert()
    {
        var bank = TestData.Bank();
        Context.Add(bank);
        Context.Add(TestData.Account(bank.Id, "First", 1));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Context.Add(TestData.Account(bank.Id, "Second", 1));

        var action = () => Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
    }

    [Fact]
    public async Task Database_WhenAccountNamesDifferOnlyByCaseAndAccent_RejectsSecondInsert()
    {
        var bank = TestData.Bank();
        Context.Add(bank);
        Context.Add(TestData.Account(bank.Id, "Épargne", 1));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Context.Add(TestData.Account(bank.Id, "EPARGNE", 2));

        var action = () => Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
    }

    [Fact]
    public async Task Database_WhenBudgetNamesDifferOnlyByAccent_RejectsSecondInsert()
    {
        var owner = await TestData.AddUserAsync(Context, "unique-budget-owner");
        Context.Add(Budget.Create("Café", owner.Id));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Context.Add(Budget.Create("Cafe", owner.Id));

        var action = () => Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
    }

    [Fact]
    public async Task Database_WhenCategoryNamesDifferOnlyByAccent_RejectsSecondInsert()
    {
        Context.Add(BudgetCategory.Create("Café", null));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Context.Add(BudgetCategory.Create("Cafe", null));

        var action = () => Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
    }

    [Fact]
    public async Task Database_WhenTransactionNamesDifferOnlyByAccentInSameScope_RejectsSecondInsert()
    {
        var (owner, budget, category, _, account) = await TestData.AddBudgetGraphAsync(Context, CurrentUser);

        budget.AddTransaction(
            category.Id,
            account.Id,
            "Café",
            TransactionType.Expense,
            10m,
            PaymentMethod.Cash,
            null,
            owner.Id);

        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        budget.AddTransaction(
            category.Id,
            account.Id,
            "Cafe",
            TransactionType.Expense,
            11m,
            PaymentMethod.Cash,
            null,
            owner.Id);

        var action = () => Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
    }

    [Theory]
    [InlineData("Value_One", "ValueXOne", "Value_One")]
    [InlineData("Value[One]", "ValueOne", "[One]")]
    [InlineData("Path\\One", "PathOne", "Path\\One")]
    public async Task AccountSearch_WhenSearchContainsLikeSpecialCharacter_TreatsItAsLiteral(
        string matchingName,
        string nonMatchingName,
        string search)
    {
        var bank = TestData.Bank();
        Context.Add(bank);
        Context.AddRange(
            TestData.Account(bank.Id, matchingName, 1),
            TestData.Account(bank.Id, nonMatchingName, 2));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var criteria = new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
            null,
            true,
            [],
            search,
            null,
            null,
            null);

        var queries = new AccountQueries(Context, NullLogger<AccountQueries>.Instance);

        var result = await queries.SearchAsync(criteria, TestContext.Current.CancellationToken);

        Assert.Equal(matchingName, Assert.Single(result.Results).Name);
    }

    [Fact]
    public async Task AccountSearch_WhenSearchDiffersOnlyByCaseAndAccent_FindsResult()
    {
        var bank = TestData.Bank();
        Context.Add(bank);
        Context.Add(TestData.Account(bank.Id, "Épargne Été"));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var criteria = new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
            null,
            true,
            [],
            "EPARGNE ete",
            null,
            null,
            null);

        var queries = new AccountQueries(Context, NullLogger<AccountQueries>.Instance);

        var result = await queries.SearchAsync(criteria, TestContext.Current.CancellationToken);

        Assert.Equal("Épargne Été", Assert.Single(result.Results).Name);
    }

    [Fact]
    public async Task AccountSearch_WhenMultipleTermsAreProvided_UsesOrSemantics()
    {
        var bank = TestData.Bank();
        Context.Add(bank);
        Context.AddRange(
            TestData.Account(bank.Id, "Alpha", 1),
            TestData.Account(bank.Id, "Beta", 2),
            TestData.Account(bank.Id, "Gamma", 3));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var criteria = new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
            null,
            true,
            [],
            "Alpha Beta",
            null,
            null,
            null);

        var queries = new AccountQueries(Context, NullLogger<AccountQueries>.Instance);

        var result = await queries.SearchAsync(criteria, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Results.Count);
        Assert.Contains(result.Results, x => x.Name == "Alpha");
        Assert.Contains(result.Results, x => x.Name == "Beta");
        Assert.DoesNotContain(result.Results, x => x.Name == "Gamma");
    }
}
