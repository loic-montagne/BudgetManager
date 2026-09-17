using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.Account.Create;
using BudgetManager.Application.Features.Budget.Create;
using BudgetManager.Application.Features.Budget.GetById;
using BudgetManager.Application.Features.Budget.UpdateAccess;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class CoreCommandHandlerTests
{
    [Fact]
    public async Task CreateBudgetHandler_WhenCommandIsValid_CreatesOwnedBudgetAndReturnsItsId()
    {
        // Arrange

        var budgetRepository = Substitute.For<IBudgetRepository>();
        var currentUserId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(
            authenticated: true,
            userId: currentUserId);

        Budget? capturedBudget = null;
        var cancellationToken = TestContext.Current.CancellationToken;

        budgetRepository
            .CreateAsync(
                Arg.Do<Budget>(budget => capturedBudget = budget),
                cancellationToken)
            .Returns(Task.CompletedTask);

        var handler = new CreateBudgetCommandHandler(
            budgetRepository,
            currentUser);

        var command = new CreateBudgetCommand(
            " Home ");

        // Act

        var budgetId = await handler.Handle(
            command,
            cancellationToken);

        // Assert

        Assert.NotNull(capturedBudget);
        Assert.Equal(budgetId, capturedBudget.Id);
        Assert.Equal("Home", capturedBudget.Name);

        Assert.Contains(
            capturedBudget.Accesses,
            access => access.UserId == currentUserId && access.IsOwner);

        await budgetRepository
            .Received(1)
            .CreateAsync(
                capturedBudget,
                cancellationToken);
    }

    [Fact]
    public async Task CreateAccountHandler_WhenCommandIsValid_MapsCommandAndPersistsAccount()
    {
        // Arrange

        var accountRepository = Substitute.For<IAccountRepository>();
        var bankId = Guid.NewGuid();
        Account? capturedAccount = null;
        var cancellationToken = TestContext.Current.CancellationToken;

        accountRepository
            .CreateAsync(
                Arg.Do<Account>(account => capturedAccount = account),
                cancellationToken)
            .Returns(Task.CompletedTask);

        var handler = new CreateAccountCommandHandler(
            accountRepository);

        var command = new CreateAccountCommand(
            " Main ",
            "FR7630006000011234567890189",
            bankId);

        // Act

        var accountId = await handler.Handle(
            command,
            cancellationToken);

        // Assert

        Assert.NotNull(capturedAccount);
        Assert.Equal(accountId, capturedAccount.Id);
        Assert.Equal("Main", capturedAccount.Name);
        Assert.Equal(bankId, capturedAccount.BankId);

        await accountRepository
            .Received(1)
            .CreateAsync(
                capturedAccount,
                cancellationToken);
    }

    [Fact]
    public async Task UpdateBudgetAccessHandler_WhenCommandIsValid_UpdatesAggregateAndPersistsIt()
    {
        // Arrange

        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var budget = Budget.Create(
            "Budget",
            ownerId);

        var budgetRepository = Substitute.For<IBudgetRepository>();
        var budgetContext = Substitute.For<IBudgetContext>();
        var currentUser = new TestCurrentUser(
            authenticated: true,
            userId: ownerId);

        var cancellationToken = TestContext.Current.CancellationToken;

        budgetContext
            .GetRequiredAsync(
                budget.Id,
                cancellationToken)
            .Returns(budget);

        var handler = new UpdateBudgetAccessCommandHandler(
            budgetRepository,
            budgetContext,
            currentUser);

        var permissions = Permission.View | Permission.Edit;

        var command = new UpdateBudgetAccessCommand(
            budget.Id,
            targetUserId,
            permissions);

        // Act

        await handler.Handle(
            command,
            cancellationToken);

        // Assert

        Assert.Contains(
            budget.Accesses,
            access =>
                access.UserId == targetUserId &&
                access.HasAllPermissions(permissions));

        await budgetRepository
            .Received(1)
            .UpdateAsync(
                budget,
                cancellationToken);
    }

    [Fact]
    public async Task GetBudgetByIdHandler_WhenBudgetExists_PassesCurrentUserAndViewPermissionToQuery()
    {
        // Arrange

        var budgetQueries = Substitute.For<IBudgetQueries>();
        var currentUserId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(
            authenticated: true,
            userId: currentUserId);

        var budgetDto = new BudgetDto(
            budgetId,
            "Budget",
            false,
            0,
            0,
            0,
            [],
            [],
            [],
            true,
            [],
            Guid.NewGuid(),
            "Created by",
            default,
            Guid.NewGuid(),
            "Updated by",
            default);

        var cancellationToken = TestContext.Current.CancellationToken;

        budgetQueries
            .GetByIdAsync(
                budgetId,
                currentUserId,
                Permission.View,
                cancellationToken)
            .Returns(budgetDto);

        var handler = new GetBudgetByIdQueryHandler(
            budgetQueries,
            currentUser);

        var query = new GetBudgetByIdQuery(
            budgetId);

        // Act

        var result = await handler.Handle(
            query,
            cancellationToken);

        // Assert

        Assert.Same(budgetDto, result);

        await budgetQueries
            .Received(1)
            .GetByIdAsync(
                budgetId,
                currentUserId,
                Permission.View,
                cancellationToken);
    }

    [Fact]
    public async Task GetBudgetByIdHandler_WhenBudgetDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange

        var budgetQueries = Substitute.For<IBudgetQueries>();
        var currentUserId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(
            authenticated: true,
            userId: currentUserId);

        var cancellationToken = TestContext.Current.CancellationToken;

        budgetQueries
            .GetByIdAsync(
                budgetId,
                currentUserId,
                Permission.View,
                cancellationToken)
            .Returns((BudgetDto?)null);

        var handler = new GetBudgetByIdQueryHandler(
            budgetQueries,
            currentUser);

        var query = new GetBudgetByIdQuery(
            budgetId);

        // Act

        var action = () => handler.Handle(
            query,
            cancellationToken);

        // Assert

        await Assert.ThrowsAsync<NotFoundException<BudgetDto>>(action);
    }
}
