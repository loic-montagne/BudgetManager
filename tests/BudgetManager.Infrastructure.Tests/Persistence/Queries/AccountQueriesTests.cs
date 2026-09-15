using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Infrastructure.Persistence.Queries;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence.Queries;

[Collection(SqlServerCollection.Name)]
public sealed class AccountQueriesTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task GetAllAsync_ProjectsComplexTypeValues()
    {
        // Arrange

        var (_, account) =
            await TestData.AddBankAndAccountAsync(
                Context,
                bankName: "BNP",
                accountName: "Current",
                bic: "BNPAFRPP",
                accountNumber: 1);

        var queries =
            new AccountQueries(
                Context,
                NullLogger<AccountQueries>.Instance);

        // Act

        var result =
            await queries.GetAllAsync(
                TestContext.Current.CancellationToken);

        // Assert

        var dto =
            Assert.Single(result);

        Assert.Equal(
            account.Id,
            dto.Id);

        Assert.Equal(
            TestData.Iban(1),
            dto.Iban);

        Assert.Equal(
            "BNPAFRPP",
            dto.Bic);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAccountDoesNotExist_ReturnsNull()
    {
        // Arrange

        var queries =
            new AccountQueries(
                Context,
                NullLogger<AccountQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                Guid.NewGuid(),
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchAsync_WhenSearchingByIban_ExecutesAndReturnsMatch()
    {
        // Arrange

        const long accountNumber =
            123456789;

        var (_, account) =
            await TestData.AddBankAndAccountAsync(
                Context,
                accountNumber: accountNumber);

        var criteria =
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                null,
                true,
                [],
                "123456789",
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

        var dto =
            Assert.Single(result.Results);

        Assert.Equal(
            account.Id,
            dto.Id);

        Assert.Equal(
            TestData.Iban(accountNumber),
            dto.Iban);
    }

    [Fact]
    public async Task SearchAsync_WhenSearchingByBic_ExecutesAndReturnsMatch()
    {
        // Arrange

        var (_, account) =
            await TestData.AddBankAndAccountAsync(
                Context,
                bic: "BNPAFRPP");

        var criteria =
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                null,
                true,
                [],
                "BNPA",
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

        var dto =
            Assert.Single(result.Results);

        Assert.Equal(
            account.Id,
            dto.Id);
    }

    [Fact]
    public async Task SearchAsync_WhenSearchContainsLiteralPercent_TreatsPercentAsLiteral()
    {
        // Arrange

        var bank =
            TestData.Bank();

        Context.Add(bank);

        Context.AddRange(
            TestData.Account(
                bank.Id,
                "Rate 50%",
                1),
            TestData.Account(
                bank.Id,
                "Rate 500",
                2));

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                null,
                true,
                [],
                "50%",
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

        var dto =
            Assert.Single(result.Results);

        Assert.Equal(
            "Rate 50%",
            dto.Name);
    }

    [Fact]
    public async Task SearchAsync_WhenOnlyIsClosedFilterIsSpecified_AppliesFilter()
    {
        // Arrange

        var bank =
            TestData.Bank();

        var open =
            TestData.Account(
                bank.Id,
                "Open",
                1);

        var closed =
            TestData.Account(
                bank.Id,
                "Closed",
                2);

        closed.Close();

        Context.Add(bank);
        Context.AddRange(
            open,
            closed);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                true,
                true,
                [],
                null,
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

        var dto =
            Assert.Single(result.Results);

        Assert.Equal(
            closed.Id,
            dto.Id);

        Assert.Equal(
            2,
            result.TotalCount);

        Assert.Equal(
            1,
            result.FilteredCount);
    }


    [Fact]
    public async Task SearchAsync_WhenFilteringByBanks_ReturnsOnlySelectedBanks()
    {
        // Arrange

        var firstBank =
            TestData.Bank(
                "First bank",
                "BNPAFRPP");

        var secondBank =
            TestData.Bank(
                "Second bank",
                "AGRIFRPP");

        var firstAccount =
            TestData.Account(
                firstBank.Id,
                "First account",
                1);

        var secondAccount =
            TestData.Account(
                secondBank.Id,
                "Second account",
                2);

        Context.AddRange(
            firstBank,
            secondBank,
            firstAccount,
            secondAccount);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                null,
                false,
                [secondBank.Id],
                null,
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

        var dto =
            Assert.Single(result.Results);

        Assert.Equal(
            secondAccount.Id,
            dto.Id);

        Assert.Equal(
            2,
            result.TotalCount);

        Assert.Equal(
            1,
            result.FilteredCount);
    }

    [Fact]
    public async Task SearchAsync_WhenAllBanksIsTrue_IgnoresBanksFilter()
    {
        // Arrange

        var firstBank =
            TestData.Bank(
                "First bank",
                "BNPAFRPP");

        var secondBank =
            TestData.Bank(
                "Second bank",
                "AGRIFRPP");

        Context.AddRange(
            firstBank,
            secondBank,
            TestData.Account(firstBank.Id, "First account", 1),
            TestData.Account(secondBank.Id, "Second account", 2));

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                null,
                true,
                [firstBank.Id],
                null,
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
    public async Task SearchAsync_WhenSortedByIbanDescending_ReturnsExpectedOrder()
    {
        // Arrange

        var bank =
            TestData.Bank();

        Context.Add(bank);

        var first =
            TestData.Account(
                bank.Id,
                "First",
                2);

        var second =
            TestData.Account(
                bank.Id,
                "Second",
                1);

        Context.AddRange(
            first,
            second);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                null,
                true,
                [],
                null,
                null,
                null,
                [
                    new SortCriterion<AccountSortField>(
                        AccountSortField.Iban,
                        SortDirection.Descending)
                ]);

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
            [second.Id, first.Id],
            result.Results.Select(x => x.Id).ToArray());
    }
    [Theory]
    [InlineData(AccountSortField.IsClosed)]
    [InlineData(AccountSortField.Name)]
    [InlineData(AccountSortField.BankName)]
    [InlineData(AccountSortField.Iban)]
    [InlineData(AccountSortField.Bic)]
    public async Task SearchAsync_ForEverySupportedSortField_ExecutesSuccessfully(
        AccountSortField sortField)
    {
        // Arrange

        await TestData.AddBankAndAccountAsync(
            Context);

        var criteria =
            new BudgetManager.Application.Features.Account.Search.PagedSearchCriteria(
                null,
                true,
                [],
                null,
                null,
                null,
                [
                    new SortCriterion<AccountSortField>(
                        sortField,
                        SortDirection.Ascending)
                ]);

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

        Assert.Single(
            result.Results);
    }


    [Fact]
    public async Task GetByIdAsync_WhenAccountExists_ProjectsNestedBankAndAudit()
    {
        // Arrange

        var (bank, account) =
            await TestData.AddBankAndAccountAsync(
                Context);

        var queries =
            new AccountQueries(
                Context,
                NullLogger<AccountQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                account.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            account.Id,
            result.Id);

        Assert.Equal(
            bank.Id,
            result.Bank.Id);

        Assert.Equal(
            bank.Bic.Value,
            result.Bank.Bic);

        Assert.Equal(
            account.CreatedOn,
            result.CreatedOn);

        Assert.Equal(
            account.CreatedBy.ToString(),
            result.CreatedByName);

        Assert.Equal(
            account.UpdatedBy.ToString(),
            result.UpdatedByName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAuditUserExists_ProjectsFullName()
    {
        // Arrange

        var auditUser =
            TestData.User(
                "audit",
                CurrentUser.UserId);

        auditUser.FirstName = "Jane";
        auditUser.LastName = "Doe";

        Context.Add(auditUser);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var (_, account) =
            await TestData.AddBankAndAccountAsync(
                Context);

        var queries =
            new AccountQueries(
                Context,
                NullLogger<AccountQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                account.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            "Jane Doe",
            result.CreatedByName);

        Assert.Equal(
            "Jane Doe",
            result.UpdatedByName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAuditUserHasNoName_ProjectsEmail()
    {
        // Arrange

        var auditUser =
            TestData.User(
                "audit",
                CurrentUser.UserId);

        auditUser.FirstName = string.Empty;
        auditUser.LastName = string.Empty;
        auditUser.Email = "audit@example.test";

        Context.Add(auditUser);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var (_, account) =
            await TestData.AddBankAndAccountAsync(
                Context);

        var queries =
            new AccountQueries(
                Context,
                NullLogger<AccountQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                account.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            "audit@example.test",
            result.CreatedByName);

        Assert.Equal(
            "audit@example.test",
            result.UpdatedByName);
    }


}
