using BudgetManager.Application.Common;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.GetById;
using BudgetManager.Application.Features.User.GetCurrent;
using BudgetManager.Application.Features.User.Search;
using NSubstitute;
using Xunit;
using UserGetByIdDto = BudgetManager.Application.Features.User.GetById.CompleteUserDto;
using UserSearchDto = BudgetManager.Application.Features.User.Search.UserDto;

namespace BudgetManager.Application.Tests;

public sealed class UserQueryHandlerTests
{
    [Fact]
    public async Task GetUserByIdHandler_WhenUserExists_ReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IUserQueries>();
        var userId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var createdOn = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedBy = Guid.NewGuid();
        var updatedOn = DateTimeOffset.UtcNow;

        var expected = new UserGetByIdDto(
            userId,
            "user.name",
            "Last name",
            "First name",
            "user@example.com",
            "+33123456789",
            SupportedCultures.English,
            SupportedThemes.Dark,
            true,
            null,
            null,
            ["Administrator"],
            new ProfilePictureDto(
                [1, 2, 3],
                SupportedPictureFormats.WebpContentType,
                createdBy,
                "Picture created by",
                createdOn,
                updatedBy,
                "Picture updated by",
                updatedOn),
            createdBy,
            "Created by",
            createdOn,
            updatedBy,
            "Updated by",
            updatedOn);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries
            .GetCompleteByIdAsync(
                userId,
                cancellationToken)
            .Returns(expected);

        var handler = new GetUserByIdQueryHandler(
            queries);

        var query = new GetUserByIdQuery(
            userId);

        // Act

        var result = await handler.Handle(
            query,
            cancellationToken);

        // Assert

        Assert.Same(
            expected,
            result);

        await queries
            .Received(1)
            .GetCompleteByIdAsync(
                userId,
                cancellationToken);
    }

    [Fact]
    public async Task GetUserByIdHandler_WhenUserDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange

        var queries = Substitute.For<IUserQueries>();
        var userId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;

        queries
            .GetCompleteByIdAsync(
                userId,
                cancellationToken)
            .Returns((UserGetByIdDto?)null);

        var handler = new GetUserByIdQueryHandler(
            queries);

        var query = new GetUserByIdQuery(
            userId);

        // Act

        var action = () => handler.Handle(
            query,
            cancellationToken);

        // Assert

        await Assert.ThrowsAsync<NotFoundException<UserGetByIdDto>>(
            action);

        await queries
            .Received(1)
            .GetCompleteByIdAsync(
                userId,
                cancellationToken);
    }

    [Fact]
    public async Task GetCurrentUserHandler_WhenUserExists_ReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IUserQueries>();
        var currentUser = new TestCurrentUser(
            userId: Guid.NewGuid());
        var expected = new CurrentUserDto(
            currentUser.RequiredUserId,
            "First name",
            "Last name",
            "user@example.com",
            "+33123456789",
            ["Administrator", "User"],
            SupportedCultures.English,
            SupportedThemes.Dark,
            [1, 2, 3],
            SupportedPictureFormats.WebpContentType);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries
            .GetCurrentAsync(
                currentUser,
                cancellationToken)
            .Returns(expected);

        var handler = new GetCurrentUserQueryHandler(
            queries,
            currentUser);

        var query = new GetCurrentUserQuery();

        // Act

        var result = await handler.Handle(
            query,
            cancellationToken);

        // Assert

        Assert.Same(
            expected,
            result);

        await queries
            .Received(1)
            .GetCurrentAsync(
                currentUser,
                cancellationToken);
    }

    [Fact]
    public async Task GetCurrentUserHandler_WhenUserDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange

        var queries = Substitute.For<IUserQueries>();
        var currentUser = new TestCurrentUser(
            userId: Guid.NewGuid());
        var cancellationToken = TestContext.Current.CancellationToken;

        queries
            .GetCurrentAsync(
                currentUser,
                cancellationToken)
            .Returns((CurrentUserDto?)null);

        var handler = new GetCurrentUserQueryHandler(
            queries,
            currentUser);

        var query = new GetCurrentUserQuery();

        // Act

        var action = () => handler.Handle(
            query,
            cancellationToken);

        // Assert

        await Assert.ThrowsAsync<NotFoundException<CurrentUserDto>>(
            action);

        await queries
            .Received(1)
            .GetCurrentAsync(
                currentUser,
                cancellationToken);
    }

    [Fact]
    public async Task SearchUsersHandler_ForwardsCriteriaAndReturnsQueryResult()
    {
        // Arrange

        var queries = Substitute.For<IUserQueries>();
        var criteria = new Features.User.Search.PagedSearchCriteria(
            null,
            "user",
            0,
            20,
            null);
        var expected = new PagedResult<UserSearchDto>(
            [
                new UserSearchDto(
                    Guid.NewGuid(),
                    "user@example.com",
                    "Last name",
                    "First name",
                    true,
                    null,
                    null,
                    ["Administrator"])
            ],
            1,
            1,
            1,
            0,
            20);
        var cancellationToken = TestContext.Current.CancellationToken;

        queries
            .SearchAsync(
                criteria,
                cancellationToken)
            .Returns(expected);

        var handler = new SearchUsersQueryHandler(
            queries);

        var query = new SearchUsersQuery(
            criteria);

        // Act

        var result = await handler.Handle(
            query,
            cancellationToken);

        // Assert

        Assert.Same(
            expected,
            result);

        await queries
            .Received(1)
            .SearchAsync(
                criteria,
                cancellationToken);
    }
}
