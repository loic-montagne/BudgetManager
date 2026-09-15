using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Infrastructure.Persistence.Queries;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence.Queries;

[Collection(SqlServerCollection.Name)]
public sealed class BudgetCategoryQueriesTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task GetAllAsync_ProjectsAssociatedBudgetsCount()
    {
        // Arrange

        var owner =
            await TestData.AddUserAsync(
                Context,
                "owner");

        var category =
            TestData.Category();

        var budget =
            BudgetManager.Domain.Entities.Budget.Create(
                "Household",
                owner.Id);

        budget.AssociateCategory(
            category,
            owner.Id);

        Context.Add(category);
        Context.Add(budget);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new BudgetCategoryQueries(
                Context,
                NullLogger<BudgetCategoryQueries>.Instance);

        // Act

        var result =
            await queries.GetAllAsync(
                TestContext.Current.CancellationToken);

        // Assert

        var dto =
            Assert.Single(result);

        Assert.Equal(
            1,
            dto.BudgetsCount);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryDoesNotExist_ReturnsNull()
    {
        // Arrange

        var queries =
            new BudgetCategoryQueries(
                Context,
                NullLogger<BudgetCategoryQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                Guid.NewGuid(),
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchAsync_WhenSearchingDescription_ReturnsExpectedCategory()
    {
        // Arrange

        var category =
            TestData.Category(
                "Food",
                "Groceries and restaurants");

        Context.Add(category);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new PagedSearchCriteria<BudgetCategorySortField>(
                "restaurants",
                null,
                null,
                null);

        var queries =
            new BudgetCategoryQueries(
                Context,
                NullLogger<BudgetCategoryQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            category.Id,
            Assert.Single(result.Results).Id);
    }

    [Fact]
    public async Task SearchAsync_WhenSortedByBudgetsCount_ExecutesSuccessfully()
    {
        // Arrange

        var owner =
            await TestData.AddUserAsync(
                Context,
                "owner");

        var unused =
            TestData.Category(
                "Unused");

        var used =
            TestData.Category(
                "Used");

        var budget =
            BudgetManager.Domain.Entities.Budget.Create(
                "Budget",
                owner.Id);

        budget.AssociateCategory(
            used,
            owner.Id);

        Context.AddRange(
            unused,
            used);

        Context.Add(budget);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new PagedSearchCriteria<BudgetCategorySortField>(
                null,
                null,
                null,
                [
                    new SortCriterion<BudgetCategorySortField>(
                        BudgetCategorySortField.BudgetsCount,
                        SortDirection.Descending)
                ]);

        var queries =
            new BudgetCategoryQueries(
                Context,
                NullLogger<BudgetCategoryQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            used.Id,
            result.Results.First().Id);
    }
    [Theory]
    [InlineData(BudgetCategorySortField.Name)]
    [InlineData(BudgetCategorySortField.Description)]
    [InlineData(BudgetCategorySortField.BudgetsCount)]
    public async Task SearchAsync_ForEverySupportedSortField_ExecutesSuccessfully(
        BudgetCategorySortField sortField)
    {
        // Arrange

        Context.Add(
            TestData.Category());

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new PagedSearchCriteria<BudgetCategorySortField>(
                null,
                null,
                null,
                [
                    new SortCriterion<BudgetCategorySortField>(
                        sortField,
                        SortDirection.Ascending)
                ]);

        var queries =
            new BudgetCategoryQueries(
                Context,
                NullLogger<BudgetCategoryQueries>.Instance);

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
    public async Task GetByIdAsync_WhenCategoryExists_ProjectsAuditAndBudgetsCount()
    {
        // Arrange

        var owner =
            await TestData.AddUserAsync(
                Context,
                "owner");

        var category =
            TestData.Category();

        var budget =
            BudgetManager.Domain.Entities.Budget.Create(
                "Budget",
                owner.Id);

        budget.AssociateCategory(
            category,
            owner.Id);

        Context.Add(category);
        Context.Add(budget);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new BudgetCategoryQueries(
                Context,
                NullLogger<BudgetCategoryQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                category.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            1,
            result.BudgetsCount);

        Assert.Equal(
            category.CreatedOn,
            result.CreatedOn);

        Assert.Equal(
            category.CreatedBy.ToString(),
            result.CreatedByName);

        Assert.Equal(
            category.UpdatedBy.ToString(),
            result.UpdatedByName);
    }


}
