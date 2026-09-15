using BudgetManager.Domain.Enums;
using BudgetManager.Domain.ValueObjects;
using BudgetManager.Infrastructure.Persistence.Queries;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence.Queries;

[Collection(SqlServerCollection.Name)]
public sealed class TransactionQueriesTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task GetByIdAsync_ProjectsSignedAmountAndComplexAccountValues()
    {
        // Arrange

        var (owner, budget, _, bank, account) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var transaction =
            budget.Transactions.Single();

        var queries =
            new TransactionQueries(
                Context);

        // Act

        var result =
            await queries.GetByIdAsync(
                transaction.Id,
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            2500m,
            result.Amount);

        Assert.Equal(
            2500m,
            result.SignedAmount);

        Assert.Equal(
            account.Iban.Value,
            result.Account.Iban);

        Assert.Equal(
            bank.Bic.Value,
            result.Account.Bic);

        Assert.Null(
            result.TransferAccount);

        Assert.Equal(
            "First Last",
            result.CreatedByName);

        Assert.Equal(
            "First Last",
            result.UpdatedByName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExpense_ProjectsNegativeSignedAmount()
    {
        // Arrange

        var (owner, budget, category, _, account) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var expense =
            budget.AddTransaction(
                category.Id,
                account.Id,
                "Expense",
                TransactionType.Expense,
                42m,
                PaymentMethod.Cash,
                null,
                owner.Id);

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new TransactionQueries(
                Context);

        // Act

        var result =
            await queries.GetByIdAsync(
                expense.Id,
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            -42m,
            result.SignedAmount);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBankTransfer_ProjectsTransferAccount()
    {
        // Arrange

        var (owner, budget, category, bank, account) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var transferAccount =
            BudgetManager.Domain.Entities.Account.Create(
                "Savings",
                bank.Id,
                Iban.Create(
                    "FR1420041010050500013M02606"));

        Context.Add(
            transferAccount);

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
            new TransactionQueries(
                Context);

        // Act

        var result =
            await queries.GetByIdAsync(
                transfer.Id,
                owner.Id,
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);
        Assert.NotNull(result.TransferAccount);

        Assert.Equal(
            transferAccount.Id,
            result.TransferAccount.Id);

        Assert.Equal(
            transferAccount.Iban.Value,
            result.TransferAccount.Iban);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserHasNoRequiredBudgetPermission_ReturnsNull()
    {
        // Arrange

        var (_, budget, _, _, _) =
            await TestData.AddBudgetGraphAsync(
                Context,
                CurrentUser);

        var transaction =
            budget.Transactions.Single();

        var queries =
            new TransactionQueries(
                Context);

        // Act

        var result =
            await queries.GetByIdAsync(
                transaction.Id,
                Guid.NewGuid(),
                Permission.View,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Null(result);
    }
}
