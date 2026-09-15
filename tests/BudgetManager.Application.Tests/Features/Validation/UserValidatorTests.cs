using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Features.User.GetById;
using BudgetManager.Application.Features.User.GetCurrent;
using BudgetManager.Application.Features.User.Search;
using FluentValidation.TestHelper;
using Xunit;

namespace BudgetManager.Application.Tests;

public sealed class UserValidatorTests
{
    [Fact]
    public async Task GetUserByIdValidator_WhenIdentifierIsEmpty_ReturnsExpectedError()
    {
        // Arrange

        var validator = new GetUserByIdQueryValidator();
        var query = new GetUserByIdQuery(
            Guid.Empty);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await validator.TestValidateAsync(
            query,
            cancellationToken: cancellationToken);

        // Assert

        result
            .ShouldHaveValidationErrorFor(item => item.Id)
            .WithErrorCode(ErrorCodes.UserIdRequired);
    }

    [Fact]
    public async Task GetUserByIdValidator_WhenIdentifierIsValid_HasNoErrors()
    {
        // Arrange

        var validator = new GetUserByIdQueryValidator();
        var query = new GetUserByIdQuery(
            Guid.NewGuid());
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await validator.TestValidateAsync(
            query,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task SearchUsersValidator_WhenPaginationIsInvalid_ReturnsExpectedErrors()
    {
        // Arrange

        var validator = new SearchUsersQueryValidator();
        var criteria = new PagedSearchCriteria(
            null,
            null,
            -1,
            0,
            null);
        var query = new SearchUsersQuery(
            criteria);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await validator.TestValidateAsync(
            query,
            cancellationToken: cancellationToken);

        // Assert

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == ErrorCodes.SearchOffsetInvalid);

        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == ErrorCodes.SearchLimitInvalid);
    }

    [Fact]
    public async Task SearchUsersValidator_WhenPaginationIsValid_HasNoErrors()
    {
        // Arrange

        var validator = new SearchUsersQueryValidator();
        var criteria = new PagedSearchCriteria(
            null,
            "user",
            0,
            20,
            null);
        var query = new SearchUsersQuery(
            criteria);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await validator.TestValidateAsync(
            query,
            cancellationToken: cancellationToken);

        // Assert

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetUserByIdQuery_RequiresAdministratorRole()
    {
        // Arrange

        var query = new GetUserByIdQuery(
            Guid.NewGuid());

        // Act

        var requiredRoles = query.RequiredRoles;

        // Assert

        Assert.Equal(
            [ApplicationRoles.Administrator],
            requiredRoles);
    }

    [Fact]
    public void GetCurrentUserQuery_RequiresAllApplicationRoles()
    {
        // Arrange

        var query = new GetCurrentUserQuery();

        // Act

        var requiredRoles = query.RequiredRoles;

        // Assert

        Assert.Equal(
            ApplicationRoles.All,
            requiredRoles);
    }

    [Fact]
    public void SearchUsersQuery_RequiresAdministratorRole()
    {
        // Arrange

        var criteria = new PagedSearchCriteria(
            null,
            null,
            null,
            null,
            null);
        var query = new SearchUsersQuery(
            criteria);

        // Act

        var requiredRoles = query.RequiredRoles;

        // Assert

        Assert.Equal(
            [ApplicationRoles.Administrator],
            requiredRoles);
    }
}
