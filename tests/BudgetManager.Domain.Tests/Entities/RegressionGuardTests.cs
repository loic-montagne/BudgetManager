using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Exceptions;
using Xunit;

namespace BudgetManager.Domain.Tests.Entities;

public sealed class RegressionGuardTests
{
    [Fact]
    public void GetPermissions_WhenUserHasPermissions_ReturnsExpectedPermissions()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.GrantAccess(userId, Permission.View, ownerId);
        budget.GrantAccess(userId, Permission.Edit, ownerId);

        // Act

        var permissions = budget.GetPermissions(
            userId,
            ownerId);

        // Assert

        Assert.Contains(Permission.View, permissions);
        Assert.Contains(Permission.Edit, permissions);
        Assert.DoesNotContain(Permission.Lock, permissions);
    }

    [Fact]
    public void GetPermissions_WhenUserHasNoAccess_ReturnsEmpty()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        // Act

        var permissions = budget.GetPermissions(
            Guid.NewGuid(),
            ownerId);

        // Assert

        Assert.Empty(permissions);
    }

    [Fact]
    public void GetPermissions_WhenCurrentUserCannotShare_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.GrantAccess(
            currentUserId,
            Permission.View,
            ownerId);

        // Act

        var action = () => budget.GetPermissions(
            Guid.NewGuid(),
            currentUserId);

        // Assert

        Assert.Throws<UnauthorizedBudgetAccessException>(action);
    }

    [Fact]
    public void HasNoPermissions_WhenUserHasNoAccess_ReturnsTrue()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        // Act

        var result = budget.HasNoPermissions(
            Guid.NewGuid(),
            ownerId);

        // Assert

        Assert.True(result);
    }

    [Fact]
    public void HasAnyPermission_WhenAtLeastOnePermissionMatches_ReturnsTrue()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.GrantAccess(
            userId,
            Permission.View,
            ownerId);

        // Act

        var result = budget.HasAnyPermission(
            userId,
            Permission.View | Permission.Edit,
            ownerId);

        // Assert

        Assert.True(result);
    }

    [Fact]
    public void HasAnyPermission_WhenNoPermissionMatches_ReturnsFalse()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        budget.GrantAccess(
            userId,
            Permission.View,
            ownerId);

        // Act

        var result = budget.HasAnyPermission(
            userId,
            Permission.Edit | Permission.Lock,
            ownerId);

        // Assert

        Assert.False(result);
    }

    [Fact]
    public void RevokeAccess_WhenTargetIsOwner_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        // Act

        var action = () => budget.RevokeAccess(
            ownerId,
            Permission.View,
            ownerId);

        // Assert

        Assert.Throws<CannotRevokeOwnerPermissionsException>(action);
    }

    [Fact]
    public void Totals_WhenBudgetContainsIncomeAndExpenses_ReturnExpectedValues()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);

        budget.AssociateCategory(category, ownerId);

        budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Income",
            TransactionType.Income,
            100m,
            PaymentMethod.Cash,
            null,
            ownerId);

        budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Expense",
            TransactionType.Expense,
            35m,
            PaymentMethod.Cash,
            null,
            ownerId);

        // Act

        var expenses = budget.Expenses;
        var incomes = budget.Incomes;
        var balance = budget.Balance;

        // Assert

        Assert.Equal(-35m, expenses);
        Assert.Equal(100m, incomes);
        Assert.Equal(65m, balance);
    }

    [Fact]
    public void Totals_WhenBudgetHasNoTransaction_ReturnZero()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);

        // Act

        var expenses = budget.Expenses;
        var incomes = budget.Incomes;
        var balance = budget.Balance;

        // Assert

        Assert.Equal(0m, expenses);
        Assert.Equal(0m, incomes);
        Assert.Equal(0m, balance);
    }

    [Fact]
    public void ChangeTransactionMethod_FromBankTransferToNonTransfer_ClearsTransferAccount()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var transferAccountId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);

        budget.AssociateCategory(category, ownerId);

        var transaction = budget.AddTransaction(
            category.Id,
            accountId,
            "Transfer",
            TransactionType.Expense,
            25m,
            PaymentMethod.BankTransfer,
            transferAccountId,
            ownerId);

        // Act

        budget.ChangeTransactionMethod(
            transaction.Id,
            PaymentMethod.Cash,
            transferAccountId,
            ownerId);

        // Assert

        Assert.Equal(PaymentMethod.Cash, transaction.Method);
        Assert.Null(transaction.TransferAccountId);
    }

    [Fact]
    public void ChangeTransactionMethod_ToBankTransferWithoutTransferAccount_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);

        budget.AssociateCategory(category, ownerId);

        var transaction = budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Operation",
            TransactionType.Expense,
            25m,
            PaymentMethod.Cash,
            null,
            ownerId);

        // Act

        var action = () => budget.ChangeTransactionMethod(
            transaction.Id,
            PaymentMethod.BankTransfer,
            null,
            ownerId);

        // Assert

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void ChangeTransactionAmount_WhenAmountIsZero_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);

        budget.AssociateCategory(category, ownerId);

        var transaction = budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Operation",
            TransactionType.Expense,
            25m,
            PaymentMethod.Cash,
            null,
            ownerId);

        // Act

        var action = () => budget.ChangeTransactionAmount(
            transaction.Id,
            0m,
            ownerId);

        // Assert

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void AddTransaction_WhenTransactionTypeIsInvalid_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);
        var invalidType = (TransactionType)42;

        budget.AssociateCategory(category, ownerId);

        // Act

        var action = () => budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Operation",
            invalidType,
            25m,
            PaymentMethod.Cash,
            null,
            ownerId);

        // Assert

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void AddTransaction_WhenPaymentMethodIsInvalid_Throws()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);
        var invalidMethod = (PaymentMethod)42;

        budget.AssociateCategory(category, ownerId);

        // Act

        var action = () => budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Operation",
            TransactionType.Expense,
            25m,
            invalidMethod,
            null,
            ownerId);

        // Assert

        Assert.Throws<ArgumentException>(action);
    }
}
