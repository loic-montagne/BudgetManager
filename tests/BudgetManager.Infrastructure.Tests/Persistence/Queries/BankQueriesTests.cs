using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Infrastructure.Persistence.Queries;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence.Queries;

[Collection(SqlServerCollection.Name)]
public sealed class BankQueriesTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task GetAllAsync_ProjectsBicAndAccountsCount()
    {
        // Arrange

        var (bank, _) =
            await TestData.AddBankAndAccountAsync(
                Context);

        var queries =
            new BankQueries(
                Context,
                NullLogger<BankQueries>.Instance);

        // Act

        var result =
            await queries.GetAllAsync(
                TestContext.Current.CancellationToken);

        // Assert

        var dto =
            Assert.Single(result);

        Assert.Equal(
            bank.Id,
            dto.Id);

        Assert.Equal(
            "BNPAFRPP",
            dto.Bic);

        Assert.Equal(
            1,
            dto.AccountsCount);
    }

    [Fact]
    public async Task SearchAsync_WhenSearchingByBic_ReturnsMatch()
    {
        // Arrange

        var bank =
            TestData.Bank(
                "BNP",
                "BNPAFRPP");

        Context.Add(bank);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new PagedSearchCriteria<BankSortField>(
                "BNPA",
                null,
                null,
                null);

        var queries =
            new BankQueries(
                Context,
                NullLogger<BankQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            bank.Id,
            Assert.Single(result.Results).Id);
    }

    [Fact]
    public async Task SearchAsync_WhenPaginationIsSpecified_ReturnsExpectedCounts()
    {
        // Arrange

        Context.AddRange(
            TestData.Bank(
                "A",
                "BNPAFRPP"),
            TestData.Bank(
                "B",
                "AGRIFRPP"),
            TestData.Bank(
                "C",
                "SOGEFRPP"));

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new PagedSearchCriteria<BankSortField>(
                null,
                1,
                1,
                [
                    new SortCriterion<BankSortField>(
                        BankSortField.Name,
                        SortDirection.Ascending)
                ]);

        var queries =
            new BankQueries(
                Context,
                NullLogger<BankQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            3,
            result.TotalCount);

        Assert.Equal(
            3,
            result.FilteredCount);

        Assert.Equal(
            1,
            result.ReturnedCount);

        Assert.Equal(
            "B",
            Assert.Single(result.Results).Name);
    }

    [Fact]
    public async Task SearchAsync_WhenSortedByAccountsCount_ExecutesSuccessfully()
    {
        // Arrange

        var firstBank =
            TestData.Bank(
                "First",
                "BNPAFRPP");

        var secondBank =
            TestData.Bank(
                "Second",
                "AGRIFRPP");

        Context.AddRange(
            firstBank,
            secondBank);

        Context.Add(
            TestData.Account(
                secondBank.Id));

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new PagedSearchCriteria<BankSortField>(
                null,
                null,
                null,
                [
                    new SortCriterion<BankSortField>(
                        BankSortField.AccountsCount,
                        SortDirection.Descending)
                ]);

        var queries =
            new BankQueries(
                Context,
                NullLogger<BankQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            secondBank.Id,
            result.Results.First().Id);
    }
    [Theory]
    [InlineData(BankSortField.Name)]
    [InlineData(BankSortField.Bic)]
    [InlineData(BankSortField.AccountsCount)]
    public async Task SearchAsync_ForEverySupportedSortField_ExecutesSuccessfully(
        BankSortField sortField)
    {
        // Arrange

        await TestData.AddBankAndAccountAsync(
            Context);

        var criteria =
            new PagedSearchCriteria<BankSortField>(
                null,
                null,
                null,
                [
                    new SortCriterion<BankSortField>(
                        sortField,
                        SortDirection.Ascending)
                ]);

        var queries =
            new BankQueries(
                Context,
                NullLogger<BankQueries>.Instance);

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
    public async Task GetByIdAsync_WhenBankExists_ProjectsAuditAndAccountsCount()
    {
        // Arrange

        var (bank, _) =
            await TestData.AddBankAndAccountAsync(
                Context);

        var queries =
            new BankQueries(
                Context,
                NullLogger<BankQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                bank.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            bank.Id,
            result.Id);

        Assert.Equal(
            1,
            result.AccountsCount);

        Assert.Equal(
            bank.CreatedBy,
            result.CreatedBy);

        Assert.Equal(
            bank.CreatedBy.ToString(),
            result.CreatedByName);

        Assert.Equal(
            bank.UpdatedBy.ToString(),
            result.UpdatedByName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBankDoesNotExist_ReturnsNull()
    {
        // Arrange

        var queries =
            new BankQueries(
                Context,
                NullLogger<BankQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                Guid.NewGuid(),
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Null(result);
    }


}
