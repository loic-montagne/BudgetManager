using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Extensions;
using BudgetManager.Infrastructure.Persistence.Queries;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence.Queries;

[Collection(SqlServerCollection.Name)]
public sealed class BudgetQueriesTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task GetAllAsync_WhenUserIsOwner_ReturnsBudget()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.GetAllAsync(
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            budget.Id,
            Assert.Single(result).Id);
    }

    [Fact]
    public async Task GetAllAsync_WhenUserHasRequiredPermission_ReturnsBudget()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var user =
            await TestData.AddUserAsync(
                Context,
                "viewer");

        budget.SetPermissions(
            user.Id,
            Permission.View,
            owner.Id);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.GetAllAsync(
                user.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            budget.Id,
            Assert.Single(result).Id);
    }

    [Fact]
    public async Task GetAllAsync_WhenUserDoesNotHaveRequiredPermission_DoesNotReturnBudget()
    {
        // Arrange

        var (owner, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var user =
            await TestData.AddUserAsync(
                Context,
                "viewer");

        budget.SetPermissions(
            user.Id,
            Permission.View,
            owner.Id);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.GetAllAsync(
                user.Id,
                Permission.Edit,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_ProjectsTotalsTransactionsCategoriesAndOwnerPermissions()
    {
        // Arrange

        var (owner, budget, category, bank, account) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                budget.Id,
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            0m,
            result.Expenses);

        Assert.Equal(
            2500m,
            result.Incomes);

        Assert.Equal(
            2500m,
            result.Balance);

        Assert.True(
            result.IsCurrentUserOwner);

        Assert.Equal(
            Permission.All.GetPermissions(),
            result.CurrentUserPermissions);

        var transaction =
            Assert.Single(result.Transactions);

        Assert.Equal(
            2500m,
            transaction.SignedAmount);

        Assert.Equal(
            account.Id,
            transaction.Account.Id);

        Assert.Equal(
            account.Name,
            transaction.Account.Name);

        Assert.Equal(
            account.IsClosed,
            transaction.Account.IsClosed);

        Assert.Equal(
            account.Iban.Value,
            transaction.Account.Iban);

        Assert.Equal(
            bank.Id,
            transaction.Account.Bank.Id);

        Assert.Equal(
            bank.Name,
            transaction.Account.Bank.Name);

        Assert.Equal(
            bank.Bic.Value,
            transaction.Account.Bank.Bic);

        Assert.Null(
            transaction.TransferAccount);

        var projectedCategory =
            Assert.Single(result.Categories);

        Assert.Equal(
            category.Id,
            projectedCategory.Id);

        Assert.Equal(
            0m,
            projectedCategory.Expenses);

        Assert.Equal(
            2500m,
            projectedCategory.Incomes);

        Assert.Equal(
            2500m,
            projectedCategory.Balance);

        Assert.Equal(
            0m,
            transaction.Category.Expenses);

        Assert.Equal(
            2500m,
            transaction.Category.Incomes);

        Assert.Equal(
            2500m,
            transaction.Category.Balance);

        Assert.Equal(
            "First Last",
            result.CreatedByName);

        Assert.Equal(
            "First Last",
            result.UpdatedByName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTransactionIsBankTransfer_ProjectsTransferAccount()
    {
        // Arrange

        var (owner, budget, category, bank, account) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var transferAccount =
            TestData.Account(
                bank.Id,
                "Savings",
                2);

        Context.Add(transferAccount);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var transfer =
            budget.AddTransaction(
                category.Id,
                account.Id,
                "Transfer",
                TransactionType.Expense,
                250m,
                PaymentMethod.BankTransfer,
                transferAccount.Id,
                owner.Id);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                budget.Id,
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        var projectedTransfer =
            Assert.Single(
                result.Transactions,
                x => x.Id == transfer.Id);

        Assert.Equal(
            account.Id,
            projectedTransfer.Account.Id);

        Assert.NotNull(
            projectedTransfer.TransferAccount);

        Assert.Equal(
            transferAccount.Id,
            projectedTransfer.TransferAccount.Id);

        Assert.Equal(
            transferAccount.Name,
            projectedTransfer.TransferAccount.Name);

        Assert.Equal(
            transferAccount.IsClosed,
            projectedTransfer.TransferAccount.IsClosed);

        Assert.Equal(
            transferAccount.Iban.Value,
            projectedTransfer.TransferAccount.Iban);

        Assert.Equal(
            bank.Id,
            projectedTransfer.TransferAccount.Bank.Id);

        Assert.Equal(
            bank.Name,
            projectedTransfer.TransferAccount.Bank.Name);

        Assert.Equal(
            bank.Bic.Value,
            projectedTransfer.TransferAccount.Bank.Bic);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBudgetHasIncomeAndExpense_ComputesTotalsInSql()
    {
        // Arrange

        var (owner, budget, category, _, account) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        budget.AddTransaction(
            category.Id,
            account.Id,
            "Rent",
            TransactionType.Expense,
            1000m,
            PaymentMethod.Cash,
            null,
            owner.Id);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                budget.Id,
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            -1000m,
            result.Expenses);

        Assert.Equal(
            2500m,
            result.Incomes);

        Assert.Equal(
            1500m,
            result.Balance);

        var projectedCategory =
            Assert.Single(result.Categories);

        Assert.Equal(
            -1000m,
            projectedCategory.Expenses);

        Assert.Equal(
            2500m,
            projectedCategory.Incomes);

        Assert.Equal(
            1500m,
            projectedCategory.Balance);

        Assert.All(
            result.Transactions,
            transaction =>
            {
                Assert.Equal(-1000m, transaction.Category.Expenses);
                Assert.Equal(2500m, transaction.Category.Incomes);
                Assert.Equal(1500m, transaction.Category.Balance);
            });

        Assert.Contains(
            result.Transactions,
            x => x.Name == "Rent" &&
                 x.SignedAmount == -1000m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserCannotView_ReturnsNull()
    {
        // Arrange

        var (_, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                budget.Id,
                Guid.NewGuid(),
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchAsync_WhenOnlyIsLockedFilterIsSpecified_AppliesFilter()
    {
        // Arrange

        var owner =
            await TestData.AddUserAsync(
                Context,
                "owner");

        var unlocked =
            BudgetManager.Domain.Entities.Budget.Create(
                "Unlocked",
                owner.Id);

        var locked =
            BudgetManager.Domain.Entities.Budget.Create(
                "Locked",
                owner.Id);

        locked.Lock(
            owner.Id);

        Context.AddRange(
            unlocked,
            locked);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new BudgetManager.Application.Features.Budget.Search.PagedSearchCriteria(
                true,
                null,
                null,
                null,
                null);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            locked.Id,
            Assert.Single(result.Results).Id);

        Assert.Equal(
            2,
            result.TotalCount);

        Assert.Equal(
            1,
            result.FilteredCount);
    }

    [Fact]
    public async Task SearchAsync_WhenMultipleSortsAreSpecified_AppliesThemInOrder()
    {
        // Arrange

        var owner =
            await TestData.AddUserAsync(
                Context,
                "owner");

        var alpha =
            BudgetManager.Domain.Entities.Budget.Create(
                "Alpha",
                owner.Id);

        var beta =
            BudgetManager.Domain.Entities.Budget.Create(
                "Beta",
                owner.Id);

        beta.Lock(
            owner.Id);

        var gamma =
            BudgetManager.Domain.Entities.Budget.Create(
                "Gamma",
                owner.Id);

        gamma.Lock(
            owner.Id);

        Context.AddRange(
            alpha,
            beta,
            gamma);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new BudgetManager.Application.Features.Budget.Search.PagedSearchCriteria(
                null,
                null,
                null,
                null,
                [
                    new SortCriterion<BudgetSortField>(
                        BudgetSortField.IsLocked,
                        SortDirection.Descending),
                    new SortCriterion<BudgetSortField>(
                        BudgetSortField.Name,
                        SortDirection.Descending)
                ]);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            ["Gamma", "Beta", "Alpha"],
            result.Results.Select(x => x.Name).ToArray());
    }
    [Fact]
    public async Task GetByIdAsync_WhenBudgetHasNoTransaction_ReturnsZeroTotals()
    {
        // Arrange

        var owner =
            await TestData.AddUserAsync(
                Context,
                "owner");

        var budget =
            BudgetManager.Domain.Entities.Budget.Create(
                "Empty",
                owner.Id);

        Context.Add(budget);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                budget.Id,
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            0m,
            result.Expenses);

        Assert.Equal(
            0m,
            result.Incomes);

        Assert.Equal(
            0m,
            result.Balance);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAssociatedCategoryHasNoTransaction_ReturnsCategoryWithZeroTotals()
    {
        // Arrange

        var owner =
            await TestData.AddUserAsync(
                Context,
                "owner");

        var category =
            TestData.Category(
                "Unused");

        var budget =
            BudgetManager.Domain.Entities.Budget.Create(
                "Budget",
                owner.Id);

        budget.AssociateCategory(
            category.Id,
            owner.Id);

        Context.Add(category);
        Context.Add(budget);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                budget.Id,
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        var projectedCategory =
            Assert.Single(result.Categories);

        Assert.Equal(
            category.Id,
            projectedCategory.Id);

        Assert.Equal(
            0m,
            projectedCategory.Expenses);

        Assert.Equal(
            0m,
            projectedCategory.Incomes);

        Assert.Equal(
            0m,
            projectedCategory.Balance);
    }


    [Theory]
    [InlineData(BudgetSortField.Name)]
    [InlineData(BudgetSortField.IsLocked)]
    public async Task SearchAsync_ForEverySupportedSortField_ExecutesSuccessfully(
        BudgetSortField sortField)
    {
        // Arrange

        var owner =
            await TestData.AddUserAsync(
                Context,
                "owner");

        Context.Add(
            BudgetManager.Domain.Entities.Budget.Create(
                "Budget",
                owner.Id));

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new BudgetManager.Application.Features.Budget.Search.PagedSearchCriteria(
                null,
                null,
                null,
                null,
                [
                    new SortCriterion<BudgetSortField>(
                        sortField,
                        SortDirection.Ascending)
                ]);

        var queries =
            new BudgetQueries(
                Context,
                NullLogger<BudgetQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Single(
            result.Results);
    }




    [Fact]
    public async Task GetByIdAsync_WhenCategoriesAreReordered_ReturnsCategoriesByOrder()
    {
        var owner = await TestData.AddUserAsync(Context, "owner-order");
        var firstCategory = TestData.Category("First");
        var secondCategory = TestData.Category("Second");
        var thirdCategory = TestData.Category("Third");
        var budget = BudgetManager.Domain.Entities.Budget.Create("Budget", owner.Id);
        budget.AssociateCategory(firstCategory.Id, owner.Id);
        budget.AssociateCategory(secondCategory.Id, owner.Id);
        budget.AssociateCategory(thirdCategory.Id, owner.Id);
        budget.ReorderCategories([thirdCategory.Id, firstCategory.Id, secondCategory.Id], owner.Id);
        Context.AddRange(firstCategory, secondCategory, thirdCategory, budget);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var queries = new BudgetQueries(Context, NullLogger<BudgetQueries>.Instance);

        var result = await queries.GetByIdAsync(
            budget.Id,
            owner.Id,
            Permission.View,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Collection(
            result.Categories,
            category => { Assert.Equal(thirdCategory.Id, category.Id); Assert.Equal(0, category.Order); },
            category => { Assert.Equal(firstCategory.Id, category.Id); Assert.Equal(1, category.Order); },
            category => { Assert.Equal(secondCategory.Id, category.Id); Assert.Equal(2, category.Order); });
    }

}
