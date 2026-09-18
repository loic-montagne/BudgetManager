using BudgetManager.Domain.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Exceptions;
using Xunit;

namespace BudgetManager.Domain.Tests.Entities;

public sealed class TransactionTests
{
    [Theory]
    [InlineData(TransactionType.Expense, "-42.5")]
    [InlineData(TransactionType.Income, "42.5")]
    public void AddTransaction_ComputesSignedAmount(TransactionType type, string expectedValue)
    {
        var (budget, ownerId, category) = CreateEditableBudget();

        var transaction = budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Opération",
            type,
            42.5m,
            PaymentMethod.CreditCard,
            null,
            ownerId);

        Assert.Equal(
            decimal.Parse(
                expectedValue,
                System.Globalization.CultureInfo.InvariantCulture),
            transaction.SignedAmount);
        Assert.Single(budget.Transactions);
    }

    [Fact]
    public void BankTransfer_RequiresDifferentTransferAccount()
    {
        var (budget, ownerId, category) = CreateEditableBudget();
        var accountId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => budget.AddTransaction(
            category.Id, accountId, "Virement", TransactionType.Expense, 10m,
            PaymentMethod.BankTransfer, null, ownerId));

        Assert.Throws<CannotTransferToSameAccountException>(() => budget.AddTransaction(
            category.Id, accountId, "Virement", TransactionType.Expense, 10m,
            PaymentMethod.BankTransfer, accountId, ownerId));
    }

    [Fact]
    public void NonTransferMethod_DiscardsTransferAccount()
    {
        var (budget, ownerId, category) = CreateEditableBudget();

        var transaction = budget.AddTransaction(
            category.Id, Guid.NewGuid(), "Carte", TransactionType.Expense, 10m,
            PaymentMethod.CreditCard, Guid.NewGuid(), ownerId);

        Assert.Null(transaction.TransferAccountId);
    }

    [Fact]
    public void Transaction_CanBeChangedThroughBudget()
    {
        var (budget, ownerId, category) = CreateEditableBudget();
        var accountId = Guid.NewGuid();
        var transferAccountId = Guid.NewGuid();
        var transaction = budget.AddTransaction(
            category.Id, accountId, "Initiale", TransactionType.Expense, 10m,
            PaymentMethod.Cash, null, ownerId);

        budget.RenameTransaction(transaction.Id, "Modifiée", ownerId);
        budget.ChangeTransactionAmount(transaction.Id, 25m, ownerId);
        budget.ChangeTransactionMethod(transaction.Id, PaymentMethod.BankTransfer, transferAccountId, ownerId);

        Assert.Equal("Modifiée", transaction.Name);
        Assert.Equal(25m, transaction.Amount.ToDecimal());
        Assert.Equal(PaymentMethod.BankTransfer, transaction.Method);
        Assert.Equal(transferAccountId, transaction.TransferAccountId);
    }

    [Fact]
    public void AddTransaction_RequiresAssociatedCategory()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        Assert.Throws<BudgetCategoryNotAssociatedException>(() => budget.AddTransaction(
            Guid.NewGuid(), Guid.NewGuid(), "Opération", TransactionType.Expense, 10m,
            PaymentMethod.Cash, null, ownerId));
    }

    private static (Budget Budget, Guid OwnerId, BudgetCategory Category) CreateEditableBudget()
    {
        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Courses", null);
        budget.AssociateCategory(category.Id, ownerId);
        return (budget, ownerId, category);
    }

    [Fact]
    public void AddTransaction_WhenNameHasMaximumLength_AddsTransaction()
    {
        // Arrange

        var (budget, ownerId, category) =
            CreateEditableBudget();

        var name = new string(
            'T',
            StringPropertyLengths.NameLength);

        // Act

        var transaction =
            budget.AddTransaction(
                category.Id,
                Guid.NewGuid(),
                name,
                TransactionType.Expense,
                10m,
                PaymentMethod.Cash,
                null,
                ownerId);

        // Assert

        Assert.Equal(
            name,
            transaction.Name);
    }

    [Fact]
    public void AddTransaction_WhenNameExceedsMaximumLength_Throws()
    {
        // Arrange

        var (budget, ownerId, category) =
            CreateEditableBudget();

        var name = new string(
            'T',
            StringPropertyLengths.NameLength + 1);

        // Act

        var action = () =>
            budget.AddTransaction(
                category.Id,
                Guid.NewGuid(),
                name,
                TransactionType.Expense,
                10m,
                PaymentMethod.Cash,
                null,
                ownerId);

        // Assert

        Assert.Throws<ArgumentException>(
            action);
    }

    [Fact]
    public void RenameTransaction_WhenNameHasMaximumLength_RenamesTransaction()
    {
        // Arrange

        var (budget, ownerId, category) =
            CreateEditableBudget();

        var transaction =
            budget.AddTransaction(
                category.Id,
                Guid.NewGuid(),
                "Initiale",
                TransactionType.Expense,
                10m,
                PaymentMethod.Cash,
                null,
                ownerId);

        var name = new string(
            'T',
            StringPropertyLengths.NameLength);

        // Act

        budget.RenameTransaction(
            transaction.Id,
            name,
            ownerId);

        // Assert

        Assert.Equal(
            name,
            transaction.Name);
    }

    [Fact]
    public void RenameTransaction_WhenNameExceedsMaximumLength_Throws()
    {
        // Arrange

        var (budget, ownerId, category) =
            CreateEditableBudget();

        var transaction =
            budget.AddTransaction(
                category.Id,
                Guid.NewGuid(),
                "Initiale",
                TransactionType.Expense,
                10m,
                PaymentMethod.Cash,
                null,
                ownerId);

        var name = new string(
            'T',
            StringPropertyLengths.NameLength + 1);

        // Act

        var action = () =>
            budget.RenameTransaction(
                transaction.Id,
                name,
                ownerId);

        // Assert

        Assert.Throws<ArgumentException>(
            action);
    }
}
