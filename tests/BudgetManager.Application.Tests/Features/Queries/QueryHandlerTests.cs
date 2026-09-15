using BudgetManager.Application.Common;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.Account.GetAll;
using BudgetManager.Application.Features.Account.GetById;
using BudgetManager.Application.Features.Account.Search;
using BudgetManager.Application.Features.Bank.GetAll;
using BudgetManager.Application.Features.Bank.GetById;
using BudgetManager.Application.Features.Bank.Search;
using BudgetManager.Application.Features.Budget.GetAccesses;
using BudgetManager.Application.Features.Budget.GetAll;
using BudgetManager.Application.Features.Budget.Search;
using BudgetManager.Application.Features.BudgetAccess.GetByKey;
using BudgetManager.Application.Features.BudgetCategory.GetAll;
using BudgetManager.Application.Features.BudgetCategory.GetById;
using BudgetManager.Application.Features.BudgetCategory.Search;
using BudgetManager.Application.Features.Transaction.GetById;
using BudgetManager.Domain.Enums;
using NSubstitute;
using Xunit;
using AccountGetAllDto = BudgetManager.Application.Features.Account.GetAll.AccountDto;
using AccountGetByIdDto = BudgetManager.Application.Features.Account.GetById.AccountDto;
using AccountGetByIdBankDto = BudgetManager.Application.Features.Account.GetById.BankDto;
using AccountSearchDto = BudgetManager.Application.Features.Account.Search.AccountDto;
using BankGetAllDto = BudgetManager.Application.Features.Bank.GetAll.BankDto;
using BankGetByIdDto = BudgetManager.Application.Features.Bank.GetById.BankDto;
using BankSearchDto = BudgetManager.Application.Features.Bank.Search.BankDto;
using BudgetAccessByKeyDto = BudgetManager.Application.Features.BudgetAccess.GetByKey.BudgetAccessDto;
using BudgetAccessByKeyBudgetDto = BudgetManager.Application.Features.BudgetAccess.GetByKey.BudgetDto;
using BudgetAccessListDto = BudgetManager.Application.Features.Budget.GetAccesses.BudgetAccessDto;
using BudgetAccessListBudgetDto = BudgetManager.Application.Features.Budget.GetAccesses.BudgetDto;
using BudgetCategoryGetAllDto = BudgetManager.Application.Features.BudgetCategory.GetAll.BudgetCategoryDto;
using BudgetCategoryGetByIdDto = BudgetManager.Application.Features.BudgetCategory.GetById.BudgetCategoryDto;
using BudgetCategorySearchDto = BudgetManager.Application.Features.BudgetCategory.Search.BudgetCategoryDto;
using BudgetGetAllDto = BudgetManager.Application.Features.Budget.GetAll.BudgetDto;
using BudgetSearchDto = BudgetManager.Application.Features.Budget.Search.BudgetDto;
using TransactionGetByIdDto = BudgetManager.Application.Features.Transaction.GetById.TransactionDto;
using TransactionBudgetDto = BudgetManager.Application.Features.Transaction.GetById.BudgetDto;
using TransactionCategoryDto = BudgetManager.Application.Features.Transaction.GetById.BudgetCategoryDto;
using TransactionAccountDto = BudgetManager.Application.Features.Transaction.GetById.AccountDto;
using UserDto = BudgetManager.Application.Features.User.Common.UserDto;
using AccountPagedSearchCriteria = BudgetManager.Application.Features.Account.Search.PagedSearchCriteria;
using BudgetPagedSearchCriteria = BudgetManager.Application.Features.Budget.Search.PagedSearchCriteria;

namespace BudgetManager.Application.Tests;

public sealed class QueryHandlerTests
{
    [Fact]
    public async Task GetAllAccountsHandler_ReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IAccountQueries>();
        IReadOnlyCollection<AccountGetAllDto> expected =
        [
            new AccountGetAllDto(Guid.NewGuid(), "Account", false, "IBAN", "Bank", "BIC")
        ];
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetAllAsync(cancellationToken).Returns(expected);

        var handler = new GetAllAccountsQueryHandler(queries);
        var query = new GetAllAccountsQuery();

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).GetAllAsync(cancellationToken);
    }

    [Fact]
    public async Task GetAccountByIdHandler_WhenAccountExists_ReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IAccountQueries>();
        var accountId = Guid.NewGuid();
        var expected = new AccountGetByIdDto(
            accountId,
            "Account",
            false,
            "IBAN",
            new AccountGetByIdBankDto(Guid.NewGuid(), "Bank", "BIC"),
            Guid.NewGuid(),
            "Created by",
            default,
            Guid.NewGuid(),
            "Updated by",
            default);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetByIdAsync(accountId, cancellationToken).Returns(expected);

        var handler = new GetAccountByIdQueryHandler(queries);
        var query = new GetAccountByIdQuery(accountId);

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).GetByIdAsync(accountId, cancellationToken);
    }

    [Fact]
    public async Task GetAccountByIdHandler_WhenAccountDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange

        var queries = Substitute.For<IAccountQueries>();
        var accountId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetByIdAsync(accountId, cancellationToken).Returns((AccountGetByIdDto?)null);

        var handler = new GetAccountByIdQueryHandler(queries);
        var query = new GetAccountByIdQuery(accountId);

        // Act

        var action = () => handler.Handle(query, cancellationToken);

        // Assert

        await Assert.ThrowsAsync<NotFoundException<AccountGetByIdDto>>(action);
    }

    [Fact]
    public async Task SearchAccountsHandler_ForwardsCriteriaAndReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IAccountQueries>();
        var criteria = new AccountPagedSearchCriteria(false, true, [], "account", 1, 10, null);
        var expected = new PagedResult<AccountSearchDto>(
            [new AccountSearchDto(Guid.NewGuid(), "Account", false, "IBAN", "Bank", "BIC")],
            1,
            1,
            1,
            1,
            10);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.SearchAsync(criteria, cancellationToken).Returns(expected);

        var handler = new SearchAccountsQueryHandler(queries);
        var query = new SearchAccountsQuery(criteria);

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).SearchAsync(criteria, cancellationToken);
    }

    [Fact]
    public async Task GetAllBanksHandler_ReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IBankQueries>();
        IReadOnlyCollection<BankGetAllDto> expected =
        [
            new BankGetAllDto(Guid.NewGuid(), "Bank", "BIC", 1)
        ];
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetAllAsync(cancellationToken).Returns(expected);

        var handler = new GetAllBanksQueryHandler(queries);
        var query = new GetAllBanksQuery();

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).GetAllAsync(cancellationToken);
    }

    [Fact]
    public async Task GetBankByIdHandler_WhenBankExists_ReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IBankQueries>();
        var bankId = Guid.NewGuid();
        var expected = new BankGetByIdDto(bankId, "Bank", "BIC", 1, Guid.NewGuid(), "Created by", default, Guid.NewGuid(), "Updated by", default);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetByIdAsync(bankId, cancellationToken).Returns(expected);

        var handler = new GetBankByIdQueryHandler(queries);
        var query = new GetBankByIdQuery(bankId);

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).GetByIdAsync(bankId, cancellationToken);
    }

    [Fact]
    public async Task GetBankByIdHandler_WhenBankDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange

        var queries = Substitute.For<IBankQueries>();
        var bankId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetByIdAsync(bankId, cancellationToken).Returns((BankGetByIdDto?)null);

        var handler = new GetBankByIdQueryHandler(queries);
        var query = new GetBankByIdQuery(bankId);

        // Act

        var action = () => handler.Handle(query, cancellationToken);

        // Assert

        await Assert.ThrowsAsync<NotFoundException<BankGetByIdDto>>(action);
    }

    [Fact]
    public async Task SearchBanksHandler_ForwardsCriteriaAndReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IBankQueries>();
        var criteria = new PagedSearchCriteria<BankSortField>("bank", 0, 20, null);
        var expected = new PagedResult<BankSearchDto>(
            [new BankSearchDto(Guid.NewGuid(), "Bank", "BIC", 1)],
            1,
            1,
            1,
            0,
            20);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.SearchAsync(criteria, cancellationToken).Returns(expected);

        var handler = new SearchBanksQueryHandler(queries);
        var query = new SearchBanksQuery(criteria);

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).SearchAsync(criteria, cancellationToken);
    }

    [Fact]
    public async Task GetAllBudgetsHandler_ForwardsCurrentUserAndViewPermission()
    {
        // Arrange

        var queries = Substitute.For<IBudgetQueries>();
        var currentUserId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(true, currentUserId);
        IReadOnlyCollection<BudgetGetAllDto> expected =
        [
            new BudgetGetAllDto(Guid.NewGuid(), "Budget", false)
        ];
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetAllAsync(currentUserId, Permission.View, cancellationToken).Returns(expected);

        var handler = new GetAllBudgetsQueryHandler(queries, currentUser);
        var query = new GetAllBudgetsQuery();

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).GetAllAsync(currentUserId, Permission.View, cancellationToken);
    }

    [Fact]
    public async Task SearchBudgetsHandler_ForwardsCriteriaCurrentUserAndViewPermission()
    {
        // Arrange

        var queries = Substitute.For<IBudgetQueries>();
        var currentUserId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(true, currentUserId);
        var criteria = new BudgetPagedSearchCriteria(false, "budget", 0, 10, null);
        var expected = new PagedResult<BudgetSearchDto>(
            [new BudgetSearchDto(Guid.NewGuid(), "Budget", false)],
            1,
            1,
            1,
            0,
            10);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.SearchAsync(criteria, currentUserId, Permission.View, cancellationToken).Returns(expected);

        var handler = new SearchBudgetsQueryHandler(queries, currentUser);
        var query = new SearchBudgetsQuery(criteria);

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).SearchAsync(criteria, currentUserId, Permission.View, cancellationToken);
    }

    [Fact]
    public async Task GetBudgetAccessesHandler_ForwardsBudgetCurrentUserAndSharePermission()
    {
        // Arrange

        var queries = Substitute.For<IBudgetAccessQueries>();
        var currentUserId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(true, currentUserId);
        IReadOnlyCollection<BudgetAccessListDto> expected =
        [
            new BudgetAccessListDto(
                new BudgetAccessListBudgetDto(budgetId, "Budget"),
                new UserDto(
                    Guid.NewGuid(),
                    "UserName",
                    "Doe",
                    "John"),
                false,
                [Permission.View])
        ];
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetByBudgetIdAsync(budgetId, currentUserId, Permission.Share, cancellationToken).Returns(expected);

        var handler = new GetBudgetAccessesQueryHandler(queries, currentUser);
        var query = new GetBudgetAccessesQuery(budgetId);

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).GetByBudgetIdAsync(budgetId, currentUserId, Permission.Share, cancellationToken);
    }

    [Fact]
    public async Task GetBudgetAccessByKeyHandler_WhenAccessExists_ReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IBudgetAccessQueries>();
        var currentUserId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(true, currentUserId);
        var expected = new BudgetAccessByKeyDto(
            new BudgetAccessByKeyBudgetDto(budgetId, "Budget"),
            new UserDto(
                userId,
                "UserName",
                "Doe",
                "John"),
            false,
            [Permission.View],
            Guid.NewGuid(),
            "Created by",
            default,
            Guid.NewGuid(),
            "Updated by",
            default);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetByKeyAsync(budgetId, userId, currentUserId, Permission.Share, cancellationToken).Returns(expected);

        var handler = new GetBudgetAccessByKeyQueryHandler(queries, currentUser);
        var query = new GetBudgetAccessByKeyQuery(budgetId, userId);

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).GetByKeyAsync(budgetId, userId, currentUserId, Permission.Share, cancellationToken);
    }

    [Fact]
    public async Task GetBudgetAccessByKeyHandler_WhenAccessDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange

        var queries = Substitute.For<IBudgetAccessQueries>();
        var currentUserId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(true, currentUserId);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetByKeyAsync(budgetId, userId, currentUserId, Permission.Share, cancellationToken).Returns((BudgetAccessByKeyDto?)null);

        var handler = new GetBudgetAccessByKeyQueryHandler(queries, currentUser);
        var query = new GetBudgetAccessByKeyQuery(budgetId, userId);

        // Act

        var action = () => handler.Handle(query, cancellationToken);

        // Assert

        await Assert.ThrowsAsync<NotFoundException<BudgetAccessByKeyDto>>(action);
    }

    [Fact]
    public async Task GetAllBudgetCategoriesHandler_ReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IBudgetCategoryQueries>();
        IReadOnlyCollection<BudgetCategoryGetAllDto> expected =
        [
            new BudgetCategoryGetAllDto(Guid.NewGuid(), "Category", "Description", 1)
        ];
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetAllAsync(cancellationToken).Returns(expected);

        var handler = new GetAllBudgetCategoriesQueryHandler(queries);
        var query = new GetAllBudgetCategoriesQuery();

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).GetAllAsync(cancellationToken);
    }

    [Fact]
    public async Task GetBudgetCategoryByIdHandler_WhenCategoryExists_ReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IBudgetCategoryQueries>();
        var categoryId = Guid.NewGuid();
        var expected = new BudgetCategoryGetByIdDto(categoryId, "Category", "Description", 1, Guid.NewGuid(), "Created by", default, Guid.NewGuid(), "Updated by", default);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetByIdAsync(categoryId, cancellationToken).Returns(expected);

        var handler = new GetBudgetCategoryByIdQueryHandler(queries);
        var query = new GetBudgetCategoryByIdQuery(categoryId);

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).GetByIdAsync(categoryId, cancellationToken);
    }

    [Fact]
    public async Task GetBudgetCategoryByIdHandler_WhenCategoryDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange

        var queries = Substitute.For<IBudgetCategoryQueries>();
        var categoryId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetByIdAsync(categoryId, cancellationToken).Returns((BudgetCategoryGetByIdDto?)null);

        var handler = new GetBudgetCategoryByIdQueryHandler(queries);
        var query = new GetBudgetCategoryByIdQuery(categoryId);

        // Act

        var action = () => handler.Handle(query, cancellationToken);

        // Assert

        await Assert.ThrowsAsync<NotFoundException<BudgetCategoryGetByIdDto>>(action);
    }

    [Fact]
    public async Task SearchBudgetCategoriesHandler_ForwardsCriteriaAndReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IBudgetCategoryQueries>();
        var criteria = new PagedSearchCriteria<BudgetCategorySortField>("category", 0, 10, null);
        var expected = new PagedResult<BudgetCategorySearchDto>(
            [new BudgetCategorySearchDto(Guid.NewGuid(), "Category", "Description", 1)],
            1,
            1,
            1,
            0,
            10);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.SearchAsync(criteria, cancellationToken).Returns(expected);

        var handler = new SearchBudgetCategoriesQueryHandler(queries);
        var query = new SearchBudgetCategoriesQuery(criteria);

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).SearchAsync(criteria, cancellationToken);
    }

    [Fact]
    public async Task GetTransactionByIdHandler_WhenTransactionExists_ForwardsCurrentUserAndViewPermission()
    {
        // Arrange

        var queries = Substitute.For<ITransactionQueries>();
        var currentUserId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(true, currentUserId);
        var account = new TransactionAccountDto(Guid.NewGuid(), "Account", false, "IBAN", "Bank", "BIC");
        var expected = new TransactionGetByIdDto(
            transactionId,
            "Transaction",
            TransactionType.Expense,
            10,
            -10,
            PaymentMethod.Cash,
            new TransactionBudgetDto(Guid.NewGuid(), "Budget"),
            new TransactionCategoryDto(Guid.NewGuid(), "Category", "Description"),
            account,
            null,
            Guid.NewGuid(),
            "Created by",
            default,
            Guid.NewGuid(),
            "Updated by",
            default);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetByIdAsync(transactionId, currentUserId, Permission.View, cancellationToken).Returns(expected);

        var handler = new GetTransactionByIdQueryHandler(queries, currentUser);
        var query = new GetTransactionByIdQuery(transactionId);

        // Act

        var result = await handler.Handle(query, cancellationToken);

        // Assert

        Assert.Same(expected, result);
        await queries.Received(1).GetByIdAsync(transactionId, currentUserId, Permission.View, cancellationToken);
    }

    [Fact]
    public async Task GetTransactionByIdHandler_WhenTransactionDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange

        var queries = Substitute.For<ITransactionQueries>();
        var currentUserId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var currentUser = new TestCurrentUser(true, currentUserId);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries.GetByIdAsync(transactionId, currentUserId, Permission.View, cancellationToken).Returns((TransactionGetByIdDto?)null);

        var handler = new GetTransactionByIdQueryHandler(queries, currentUser);
        var query = new GetTransactionByIdQuery(transactionId);

        // Act

        var action = () => handler.Handle(query, cancellationToken);

        // Assert

        await Assert.ThrowsAsync<NotFoundException<TransactionGetByIdDto>>(action);
    }
}
