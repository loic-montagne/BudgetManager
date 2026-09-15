using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Features.Account.Close;
using BudgetManager.Application.Features.Account.Delete;
using BudgetManager.Application.Features.Account.Update;
using BudgetManager.Application.Features.Bank.Create;
using BudgetManager.Application.Features.Bank.Delete;
using BudgetManager.Application.Features.Bank.Update;
using BudgetManager.Application.Features.Budget.AssociateCategory;
using BudgetManager.Application.Features.Budget.Delete;
using BudgetManager.Application.Features.Budget.DissociateCategory;
using BudgetManager.Application.Features.Budget.Lock;
using BudgetManager.Application.Features.Budget.TransferOwnership;
using BudgetManager.Application.Features.Budget.Unlock;
using BudgetManager.Application.Features.Budget.Update;
using BudgetManager.Application.Features.BudgetCategory.Create;
using BudgetManager.Application.Features.BudgetCategory.Delete;
using BudgetManager.Application.Features.BudgetCategory.Update;
using BudgetManager.Application.Features.Transaction.Create;
using BudgetManager.Application.Features.Transaction.Delete;
using BudgetManager.Application.Features.Transaction.Update;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class CommandHandlerTests
{
    [Fact]
    public async Task UpdateAccountHandler_WhenCommandIsValid_RenamesAndPersistsAccount()
    {
        // Arrange

        var account = Account.Create(
            "Old name",
            Guid.NewGuid(),
            Iban.Create("FR7630006000011234567890189"));

        var accountContext = Substitute.For<IAccountContext>();
        var accountRepository = Substitute.For<IAccountRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        accountContext
            .GetRequiredAsync(account.Id, cancellationToken)
            .Returns(account);

        var handler = new UpdateAccountCommandHandler(
            accountContext,
            accountRepository);

        var command = new UpdateAccountCommand(
            account.Id,
            " New name ");

        // Act

        await handler.Handle(command, cancellationToken);

        // Assert

        Assert.Equal("New name", account.Name);

        await accountRepository
            .Received(1)
            .UpdateAsync(account, cancellationToken);
    }

    [Fact]
    public async Task CloseAccountHandler_WhenCommandIsValid_ClosesAndPersistsAccount()
    {
        // Arrange

        var account = Account.Create(
            "Account",
            Guid.NewGuid(),
            Iban.Create("FR7630006000011234567890189"));

        var accountContext = Substitute.For<IAccountContext>();
        var accountRepository = Substitute.For<IAccountRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        accountContext
            .GetRequiredAsync(account.Id, cancellationToken)
            .Returns(account);

        var handler = new CloseAccountCommandHandler(
            accountContext,
            accountRepository);

        var command = new CloseAccountCommand(account.Id);

        // Act

        await handler.Handle(command, cancellationToken);

        // Assert

        Assert.True(account.IsClosed);

        await accountRepository
            .Received(1)
            .UpdateAsync(account, cancellationToken);
    }

    [Fact]
    public async Task DeleteAccountHandler_WhenCommandIsValid_DeletesAccount()
    {
        // Arrange

        var account = Account.Create(
            "Account",
            Guid.NewGuid(),
            Iban.Create("FR7630006000011234567890189"));

        var accountContext = Substitute.For<IAccountContext>();
        var accountRepository = Substitute.For<IAccountRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        accountContext
            .GetRequiredAsync(account.Id, cancellationToken)
            .Returns(account);

        var handler = new DeleteAccountCommandHandler(
            accountContext,
            accountRepository);

        var command = new DeleteAccountCommand(account.Id);

        // Act

        await handler.Handle(command, cancellationToken);

        // Assert

        await accountRepository
            .Received(1)
            .DeleteAsync(account, cancellationToken);
    }

    [Fact]
    public async Task CreateBankHandler_WhenCommandIsValid_CreatesAndPersistsBank()
    {
        // Arrange

        var bankRepository = Substitute.For<IBankRepository>();
        Bank? capturedBank = null;
        var cancellationToken = TestContext.Current.CancellationToken;

        bankRepository
            .CreateAsync(
                Arg.Do<Bank>(bank => capturedBank = bank),
                cancellationToken)
            .Returns(Task.CompletedTask);

        var handler = new CreateBankCommandHandler(bankRepository);
        var command = new CreateBankCommand(" Bank ", "BNPAFRPP");

        // Act

        var result = await handler.Handle(command, cancellationToken);

        // Assert

        Assert.NotNull(capturedBank);
        Assert.Equal(result, capturedBank.Id);
        Assert.Equal("Bank", capturedBank.Name);
        Assert.Equal("BNPAFRPP", capturedBank.Bic.Value);
    }

    [Fact]
    public async Task UpdateBankHandler_WhenCommandIsValid_UpdatesAndPersistsBank()
    {
        // Arrange

        var bank = Bank.Create(
            "Old bank",
            Bic.Create("BNPAFRPP"));

        var bankContext = Substitute.For<IBankContext>();
        var bankRepository = Substitute.For<IBankRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        bankContext
            .GetRequiredAsync(bank.Id, cancellationToken)
            .Returns(bank);

        var handler = new UpdateBankCommandHandler(
            bankContext,
            bankRepository);

        var command = new UpdateBankCommand(
            bank.Id,
            " New bank ",
            "AGRIFRPP");

        // Act

        await handler.Handle(command, cancellationToken);

        // Assert

        Assert.Equal("New bank", bank.Name);
        Assert.Equal("AGRIFRPP", bank.Bic.Value);

        await bankRepository
            .Received(1)
            .UpdateAsync(bank, cancellationToken);
    }

    [Fact]
    public async Task DeleteBankHandler_WhenCommandIsValid_DeletesBank()
    {
        // Arrange

        var bank = Bank.Create(
            "Bank",
            Bic.Create("BNPAFRPP"));

        var bankContext = Substitute.For<IBankContext>();
        var bankRepository = Substitute.For<IBankRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        bankContext
            .GetRequiredAsync(bank.Id, cancellationToken)
            .Returns(bank);

        var handler = new DeleteBankCommandHandler(
            bankContext,
            bankRepository);

        var command = new DeleteBankCommand(bank.Id);

        // Act

        await handler.Handle(command, cancellationToken);

        // Assert

        await bankRepository
            .Received(1)
            .DeleteAsync(bank, cancellationToken);
    }

    [Fact]
    public async Task UpdateBudgetHandler_WhenCommandIsValid_RenamesAndPersistsBudget()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Old budget", ownerId);
        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var currentUser = new TestCurrentUser(true, ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext
            .GetRequiredAsync(budget.Id, cancellationToken)
            .Returns(budget);

        var handler = new UpdateBudgetCommandHandler(
            budgetRepository,
            budgetContext,
            currentUser);

        var command = new UpdateBudgetCommand(
            budget.Id,
            " New budget ");

        // Act

        await handler.Handle(command, cancellationToken);

        // Assert

        Assert.Equal("New budget", budget.Name);

        await budgetRepository
            .Received(1)
            .UpdateAsync(budget, cancellationToken);
    }

    [Fact]
    public async Task LockBudgetHandler_WhenCommandIsValid_LocksAndPersistsBudget()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var currentUser = new TestCurrentUser(true, ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext
            .GetRequiredAsync(budget.Id, cancellationToken)
            .Returns(budget);

        var handler = new LockBudgetCommandHandler(
            budgetRepository,
            budgetContext,
            currentUser);

        // Act

        await handler.Handle(
            new LockBudgetCommand(budget.Id),
            cancellationToken);

        // Assert

        Assert.True(budget.IsLocked);

        await budgetRepository
            .Received(1)
            .UpdateAsync(budget, cancellationToken);
    }

    [Fact]
    public async Task UnlockBudgetHandler_WhenCommandIsValid_UnlocksAndPersistsBudget()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        budget.Lock(ownerId);

        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var currentUser = new TestCurrentUser(true, ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext
            .GetRequiredAsync(budget.Id, cancellationToken)
            .Returns(budget);

        var handler = new UnlockBudgetCommandHandler(
            budgetRepository,
            budgetContext,
            currentUser);

        // Act

        await handler.Handle(
            new UnlockBudgetCommand(budget.Id),
            cancellationToken);

        // Assert

        Assert.False(budget.IsLocked);

        await budgetRepository
            .Received(1)
            .UpdateAsync(budget, cancellationToken);
    }

    [Fact]
    public async Task TransferBudgetOwnershipHandler_WhenCommandIsValid_TransfersAndPersistsBudget()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var currentUser = new TestCurrentUser(true, ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext
            .GetRequiredAsync(budget.Id, cancellationToken)
            .Returns(budget);

        var handler = new TransferBudgetOwnershipCommandHandler(
            budgetRepository,
            budgetContext,
            currentUser);

        var command = new TransferBudgetOwnershipCommand(
            budget.Id,
            newOwnerId);

        // Act

        await handler.Handle(command, cancellationToken);

        // Assert

        Assert.Contains(
            budget.Accesses,
            access => access.UserId == newOwnerId && access.IsOwner);

        await budgetRepository
            .Received(1)
            .TransferOwnershipAsync(
                budget,
                ownerId,
                cancellationToken);

        await budgetRepository
            .DidNotReceive()
            .UpdateAsync(
                Arg.Any<Budget>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssociateCategoryHandler_WhenCommandIsValid_AssociatesAndPersistsBudget()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);
        var budgetContext = Substitute.For<IBudgetContext>();
        var categoryContext = Substitute.For<IBudgetCategoryContext>();
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var currentUser = new TestCurrentUser(true, ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.GetRequiredAsync(budget.Id, cancellationToken).Returns(budget);
        categoryContext.GetRequiredAsync(category.Id, cancellationToken).Returns(category);

        var handler = new AssociateCategoryCommandHandler(
            budgetRepository,
            budgetContext,
            categoryContext,
            currentUser);

        // Act

        await handler.Handle(
            new AssociateCategoryCommand(budget.Id, category.Id),
            cancellationToken);

        // Assert

        Assert.Contains(budget.Categories, item => item.Id == category.Id);

        await budgetRepository.Received(1).UpdateAsync(budget, cancellationToken);
    }

    [Fact]
    public async Task DissociateCategoryHandler_WhenCommandIsValid_DissociatesAndPersistsBudget()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var budget = Budget.Create("Budget", ownerId);
        var category = BudgetCategory.Create("Category", null);
        budget.AssociateCategory(category, ownerId);

        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var currentUser = new TestCurrentUser(true, ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.GetRequiredAsync(budget.Id, cancellationToken).Returns(budget);

        var handler = new DissociateCategoryCommandHandler(
            budgetRepository,
            budgetContext,
            currentUser);

        // Act

        await handler.Handle(
            new DissociateCategoryCommand(budget.Id, category.Id),
            cancellationToken);

        // Assert

        Assert.DoesNotContain(budget.Categories, item => item.Id == category.Id);
        await budgetRepository.Received(1).UpdateAsync(budget, cancellationToken);
    }

    [Fact]
    public async Task DeleteBudgetHandler_WhenCommandIsValid_DeletesBudget()
    {
        // Arrange

        var budget = Budget.Create("Budget", Guid.NewGuid());
        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.GetRequiredAsync(budget.Id, cancellationToken).Returns(budget);

        var handler = new DeleteBudgetCommandHandler(
            budgetRepository,
            budgetContext);

        // Act

        await handler.Handle(new DeleteBudgetCommand(budget.Id), cancellationToken);

        // Assert

        await budgetRepository.Received(1).DeleteAsync(budget, cancellationToken);
    }

    [Fact]
    public async Task CreateBudgetCategoryHandler_WhenCommandIsValid_CreatesAndPersistsCategory()
    {
        // Arrange

        var repository = Substitute.For<IBudgetCategoryRepository>();
        BudgetCategory? capturedCategory = null;
        var cancellationToken = TestContext.Current.CancellationToken;

        repository
            .CreateAsync(
                Arg.Do<BudgetCategory>(category => capturedCategory = category),
                cancellationToken)
            .Returns(Task.CompletedTask);

        var handler = new CreateBudgetCategoryCommandHandler(repository);
        var command = new CreateBudgetCategoryCommand(" Category ", " Description ");

        // Act

        var result = await handler.Handle(command, cancellationToken);

        // Assert

        Assert.NotNull(capturedCategory);
        Assert.Equal(result, capturedCategory.Id);
        Assert.Equal("Category", capturedCategory.Name);
        Assert.Equal("Description", capturedCategory.Description);
    }

    [Fact]
    public async Task UpdateBudgetCategoryHandler_WhenCommandIsValid_UpdatesAndPersistsCategory()
    {
        // Arrange

        var category = BudgetCategory.Create("Old", "Old description");
        var context = Substitute.For<IBudgetCategoryContext>();
        var repository = Substitute.For<IBudgetCategoryRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        context.GetRequiredAsync(category.Id, cancellationToken).Returns(category);

        var handler = new UpdateBudgetCategoryCommandHandler(context, repository);
        var command = new UpdateBudgetCategoryCommand(category.Id, " New ", " New description ");

        // Act

        await handler.Handle(command, cancellationToken);

        // Assert

        Assert.Equal("New", category.Name);
        Assert.Equal("New description", category.Description);
        await repository.Received(1).UpdateAsync(category, cancellationToken);
    }

    [Fact]
    public async Task DeleteBudgetCategoryHandler_WhenCommandIsValid_DeletesCategory()
    {
        // Arrange

        var category = BudgetCategory.Create("Category", null);
        var context = Substitute.For<IBudgetCategoryContext>();
        var repository = Substitute.For<IBudgetCategoryRepository>();
        var cancellationToken = TestContext.Current.CancellationToken;

        context.GetRequiredAsync(category.Id, cancellationToken).Returns(category);

        var handler = new DeleteBudgetCategoryCommandHandler(context, repository);

        // Act

        await handler.Handle(new DeleteBudgetCategoryCommand(category.Id), cancellationToken);

        // Assert

        await repository.Received(1).DeleteAsync(category, cancellationToken);
    }

    [Fact]
    public async Task CreateTransactionHandler_WhenCommandIsValid_AddsAndPersistsTransaction()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var category = BudgetCategory.Create("Category", null);
        var budget = Budget.Create("Budget", ownerId);
        budget.AssociateCategory(category, ownerId);

        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var currentUser = new TestCurrentUser(true, ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.GetRequiredAsync(budget.Id, cancellationToken).Returns(budget);

        var handler = new CreateTransactionCommandHandler(
            budgetRepository,
            budgetContext,
            currentUser);

        var command = new CreateTransactionCommand(
            budget.Id,
            category.Id,
            Guid.NewGuid(),
            "Transaction",
            TransactionType.Expense,
            12.50m,
            PaymentMethod.Cash,
            null);

        // Act

        await handler.Handle(command, cancellationToken);

        // Assert

        var transaction = Assert.Single(budget.Transactions);
        Assert.Equal("Transaction", transaction.Name);
        Assert.Equal(12.50m, transaction.Amount.ToDecimal());
        await budgetRepository.Received(1).UpdateAsync(budget, cancellationToken);
    }

    [Fact]
    public async Task UpdateTransactionHandler_WhenCommandIsValid_UpdatesAllFieldsAndPersistsBudget()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var category = BudgetCategory.Create("Category", null);
        var budget = Budget.Create("Budget", ownerId);
        budget.AssociateCategory(category, ownerId);
        var transaction = budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Old",
            TransactionType.Expense,
            new UDecimal(10m),
            PaymentMethod.Cash,
            null,
            ownerId);

        var transferAccountId = Guid.NewGuid();
        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var currentUser = new TestCurrentUser(true, ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.GetRequiredAsync(budget.Id, cancellationToken).Returns(budget);

        var handler = new UpdateTransactionCommandHandler(
            budgetRepository,
            budgetContext,
            currentUser);

        var command = new UpdateTransactionCommand(
            budget.Id,
            transaction.Id,
            "New",
            25m,
            PaymentMethod.BankTransfer,
            transferAccountId);

        // Act

        await handler.Handle(command, cancellationToken);

        // Assert

        Assert.Equal("New", transaction.Name);
        Assert.Equal(25m, transaction.Amount.ToDecimal());
        Assert.Equal(PaymentMethod.BankTransfer, transaction.Method);
        Assert.Equal(transferAccountId, transaction.TransferAccountId);
        await budgetRepository.Received(1).UpdateAsync(budget, cancellationToken);
    }

    [Fact]
    public async Task DeleteTransactionHandler_WhenCommandIsValid_RemovesAndPersistsTransaction()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var category = BudgetCategory.Create("Category", null);
        var budget = Budget.Create("Budget", ownerId);
        budget.AssociateCategory(category, ownerId);
        var transaction = budget.AddTransaction(
            category.Id,
            Guid.NewGuid(),
            "Transaction",
            TransactionType.Expense,
            new UDecimal(10m),
            PaymentMethod.Cash,
            null,
            ownerId);

        var budgetContext = Substitute.For<IBudgetContext>();
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var currentUser = new TestCurrentUser(true, ownerId);
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext.GetRequiredAsync(budget.Id, cancellationToken).Returns(budget);

        var handler = new DeleteTransactionCommandHandler(
            budgetRepository,
            budgetContext,
            currentUser);

        // Act

        await handler.Handle(
            new DeleteTransactionCommand(budget.Id, transaction.Id),
            cancellationToken);

        // Assert

        Assert.Empty(budget.Transactions);
        await budgetRepository.Received(1).UpdateAsync(budget, cancellationToken);
    }
}
