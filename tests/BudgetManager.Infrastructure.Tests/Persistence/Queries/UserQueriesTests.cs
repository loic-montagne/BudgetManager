using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Infrastructure.Persistence.Queries;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence.Queries;

[Collection(SqlServerCollection.Name)]
public sealed class UserQueriesTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsProjectedUser()
    {
        // Arrange

        var user =
            await TestData.AddUserAsync(
                Context,
                "query");

        user.PreferredCulture = SupportedCultures.English;
        user.PreferredTheme = SupportedThemes.Dark;

        Context.Add(
            new ApplicationUserProfilePicture
            {
                UserId = user.Id,
                Content = [1, 2, 3],
                ContentType = SupportedPictureFormats.WebpContentType
            });

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                user.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            user.Id,
            result.Id);

        Assert.Equal(
            user.UserName,
            result.UserName);

        Assert.Equal(
            SupportedCultures.English,
            result.PreferredCulture);

        Assert.Equal(
            SupportedThemes.Dark,
            result.PreferredTheme);

        Assert.Equal(
            new byte[] { 1, 2, 3 },
            result.ProfilePictureContent);

        Assert.Equal(
            SupportedPictureFormats.WebpContentType,
            result.ProfilePictureContentType);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserHasNoProfilePicture_ReturnsNullPictureFields()
    {
        // Arrange

        var user =
            await TestData.AddUserAsync(
                Context,
                "query-no-picture");

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await queries.GetByIdAsync(
                user.Id,
                cancellationToken);

        // Assert

        Assert.NotNull(
            result);

        Assert.Null(
            result.ProfilePictureContent);

        Assert.Null(
            result.ProfilePictureContentType);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.GetByIdAsync(
                Guid.NewGuid(),
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCompleteByIdAsync_WhenUserHasMultipleRoles_ReturnsAllRoles()
    {
        // Arrange

        var user =
            await TestData.AddUserAsync(
                Context,
                "roles");

        var activationSentOn =
            TimeProvider.GetUtcNow().AddHours(-1);

        var activationExpiresOn =
            activationSentOn.AddDays(7);

        user.EmailConfirmed = false;
        user.ActivationEmailSentOn = activationSentOn;
        user.ActivationEmailExpiresOn = activationExpiresOn;

        var administrator =
            new ApplicationRole("Administrator")
            {
                Id = Guid.NewGuid(),
                NormalizedName = "ADMINISTRATOR"
            };

        var standardUser =
            new ApplicationRole("User")
            {
                Id = Guid.NewGuid(),
                NormalizedName = "USER"
            };

        Context.AddRange(
            administrator,
            standardUser);

        Context.AddRange(
            new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = administrator.Id
            },
            new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = standardUser.Id
            });

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.GetCompleteByIdAsync(
                user.Id,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            ["Administrator", "User"],
            result.Roles.OrderBy(x => x).ToArray());

        Assert.Equal(
            SupportedCultures.French,
            result.PreferredCulture);

        Assert.Null(
            result.PreferredTheme);

        Assert.False(
            result.EmailConfirmed);

        Assert.Equal(
            activationSentOn,
            result.ActivationEmailSentOn);

        Assert.Equal(
            activationExpiresOn,
            result.ActivationEmailExpiresOn);

        Assert.Null(
            result.ProfilePicture);
    }


    [Fact]
    public async Task GetCompleteByIdAsync_WhenPreferencesAndPictureExist_ReturnsThem()
    {
        // Arrange

        var user =
            await TestData.AddUserAsync(
                Context,
                "complete-profile");

        user.PreferredCulture =
            SupportedCultures.English;

        user.PreferredTheme =
            SupportedThemes.System;

        Context.Add(
            new ApplicationUserProfilePicture
            {
                UserId = user.Id,
                Content = [4, 5, 6],
                ContentType = SupportedPictureFormats.WebpContentType
            });

        var cancellationToken =
            TestContext.Current.CancellationToken;

        await Context.SaveChangesAsync(
            cancellationToken);

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.GetCompleteByIdAsync(
                user.Id,
                cancellationToken);

        // Assert

        Assert.NotNull(
            result);

        Assert.Equal(
            SupportedCultures.English,
            result.PreferredCulture);

        Assert.Equal(
            SupportedThemes.System,
            result.PreferredTheme);

        Assert.NotNull(
            result.ProfilePicture);

        Assert.Equal(
            new byte[] { 4, 5, 6 },
            result.ProfilePicture.Content);

        Assert.Equal(
            SupportedPictureFormats.WebpContentType,
            result.ProfilePicture.ContentType);

        Assert.Equal(
            CurrentUser.UserId!.Value,
            result.ProfilePicture.CreatedBy);

        Assert.Equal(
            TimeProvider.GetUtcNow(),
            result.ProfilePicture.CreatedOn);

        Assert.Equal(
            CurrentUser.UserId!.Value,
            result.ProfilePicture.UpdatedBy);

        Assert.Equal(
            TimeProvider.GetUtcNow(),
            result.ProfilePicture.UpdatedOn);

        Assert.Equal(
            CurrentUser.UserId.Value.ToString(),
            result.CreatedByName);

        Assert.Equal(
            CurrentUser.UserId.Value.ToString(),
            result.UpdatedByName);

        Assert.Equal(
            CurrentUser.UserId.Value.ToString(),
            result.ProfilePicture.CreatedByName);

        Assert.Equal(
            CurrentUser.UserId.Value.ToString(),
            result.ProfilePicture.UpdatedByName);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenUserExists_ReturnsProjectedCurrentUser()
    {
        // Arrange

        var user =
            await TestData.AddUserAsync(
                Context,
                "current");

        user.FirstName = "Current first";
        user.LastName = "Current last";
        user.Email = "current@example.test";
        user.PreferredCulture = SupportedCultures.English;
        user.PreferredTheme = SupportedThemes.Dark;

        var administrator =
            new ApplicationRole("Administrator")
            {
                Id = Guid.NewGuid(),
                NormalizedName = "ADMINISTRATOR"
            };

        var standardUser =
            new ApplicationRole("User")
            {
                Id = Guid.NewGuid(),
                NormalizedName = "USER"
            };

        Context.AddRange(
            administrator,
            standardUser);

        Context.AddRange(
            new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = administrator.Id
            },
            new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = standardUser.Id
            });

        Context.Add(
            new ApplicationUserProfilePicture
            {
                UserId = user.Id,
                Content = [7, 8, 9],
                ContentType = SupportedPictureFormats.WebpContentType
            });

        var cancellationToken =
            TestContext.Current.CancellationToken;

        await Context.SaveChangesAsync(
            cancellationToken);

        CurrentUser.UserId = user.Id;

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.GetCurrentAsync(
                CurrentUser,
                cancellationToken);

        // Assert

        Assert.NotNull(
            result);

        Assert.Equal(
            user.Id,
            result.Id);

        Assert.Equal(
            "Current first",
            result.FirstName);

        Assert.Equal(
            "Current last",
            result.LastName);

        Assert.Equal(
            "current@example.test",
            result.Email);

        Assert.Equal(
            ["Administrator", "User"],
            result.Roles.OrderBy(x => x).ToArray());

        Assert.Equal(
            SupportedCultures.English,
            result.PreferredCulture);

        Assert.Equal(
            SupportedThemes.Dark,
            result.PreferredTheme);

        Assert.Equal(
            new byte[] { 7, 8, 9 },
            result.ProfilePictureContent);

        Assert.Equal(
            SupportedPictureFormats.WebpContentType,
            result.ProfilePictureContentType);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenUserHasNoRolesOrProfilePicture_ReturnsEmptyRolesAndNullPictureFields()
    {
        // Arrange

        var user =
            await TestData.AddUserAsync(
                Context,
                "current-minimal");

        CurrentUser.UserId = user.Id;

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await queries.GetCurrentAsync(
                CurrentUser,
                cancellationToken);

        // Assert

        Assert.NotNull(
            result);

        Assert.Empty(
            result.Roles);

        Assert.Equal(
            SupportedCultures.French,
            result.PreferredCulture);

        Assert.Null(
            result.PreferredTheme);

        Assert.Null(
            result.ProfilePictureContent);

        Assert.Null(
            result.ProfilePictureContentType);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange

        CurrentUser.UserId = Guid.NewGuid();

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.GetCurrentAsync(
                CurrentUser,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Null(
            result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SearchAsync_WhenFilteringByActivationState_ReturnsOnlyExpectedUsers(
        bool isActivated)
    {
        // Arrange

        const string search = "activation-filter";

        var activatedUser =
            TestData.User(search);

        activatedUser.EmailConfirmed = true;

        var pendingUser =
            TestData.User(search);

        pendingUser.EmailConfirmed = false;

        Context.AddRange(
            activatedUser,
            pendingUser);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        await Context.SaveChangesAsync(
            cancellationToken);

        var criteria =
            new Application.Features.User.Search.PagedSearchCriteria(
                isActivated,
                search,
                null,
                null,
                null);

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                cancellationToken);

        // Assert

        var user =
            Assert.Single(
                result.Results);

        Assert.Equal(
            isActivated ? activatedUser.Id : pendingUser.Id,
            user.Id);

        Assert.Equal(
            isActivated,
            user.IsActivated);
    }

    [Fact]
    public async Task SearchAsync_WhenActivationFilterIsNull_ReturnsActivatedAndPendingUsers()
    {
        // Arrange

        const string search = "activation-filter-all";

        var activatedUser =
            TestData.User(search);

        activatedUser.EmailConfirmed = true;

        var pendingUser =
            TestData.User(search);

        pendingUser.EmailConfirmed = false;

        Context.AddRange(
            activatedUser,
            pendingUser);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        await Context.SaveChangesAsync(
            cancellationToken);

        var criteria =
            new Application.Features.User.Search.PagedSearchCriteria(
                null,
                search,
                null,
                null,
                null);

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                cancellationToken);

        // Assert

        Assert.Equal(
            2,
            result.FilteredCount);

        Assert.Equal(
            2,
            result.ReturnedCount);

        Assert.Equal(
            new[] { activatedUser.Id, pendingUser.Id }
                .OrderBy(x => x)
                .ToArray(),
            result.Results
                .Select(x => x.Id)
                .OrderBy(x => x)
                .ToArray());

        Assert.Contains(
            result.Results,
            x => x.Id == activatedUser.Id && x.IsActivated);

        Assert.Contains(
            result.Results,
            x => x.Id == pendingUser.Id && !x.IsActivated);
    }

    [Fact]
    public async Task SearchAsync_WhenSearchingByRole_ReturnsExpectedUser()
    {
        // Arrange

        var user =
            await TestData.AddUserAsync(
                Context,
                "admin");

        var activationSentOn =
            TimeProvider.GetUtcNow().AddHours(-2);

        var activationExpiresOn =
            activationSentOn.AddDays(7);

        user.EmailConfirmed = false;
        user.ActivationEmailSentOn = activationSentOn;
        user.ActivationEmailExpiresOn = activationExpiresOn;

        var role =
            new ApplicationRole("Administrator")
            {
                Id = Guid.NewGuid(),
                NormalizedName = "ADMINISTRATOR"
            };

        Context.Add(role);

        Context.Add(
            new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new Application.Features.User.Search.PagedSearchCriteria(
                null,
                "Administrator",
                null,
                null,
                null);

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                TestContext.Current.CancellationToken);

        // Assert

        var projectedUser =
            Assert.Single(
                result.Results);

        Assert.Equal(
            user.Id,
            projectedUser.Id);

        Assert.False(
            projectedUser.IsActivated);

        Assert.Equal(
            activationSentOn,
            projectedUser.ActivationEmailSentOn);

        Assert.Equal(
            activationExpiresOn,
            projectedUser.ActivationEmailExpiresOn);
    }

    [Fact]
    public async Task SearchAsync_WhenSearchingByEmailIsCaseInsensitive_ReturnsExpectedUser()
    {
        // Arrange

        var user =
            await TestData.AddUserAsync(
                Context,
                "mixed");

        var role =
            new ApplicationRole("User")
            {
                Id = Guid.NewGuid(),
                NormalizedName = "USER"
            };

        Context.Add(role);

        Context.Add(
            new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new Application.Features.User.Search.PagedSearchCriteria(
                null,
                user.Email!.ToUpperInvariant(),
                null,
                null,
                null);

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            user.Id,
            Assert.Single(result.Results).Id);
    }

    [Fact]
    public async Task SearchAsync_WhenSortedByEmail_ExecutesAndOrdersResults()
    {
        // Arrange

        var first =
            TestData.User("a");

        first.Email = "a@example.test";
        first.NormalizedEmail = "A@EXAMPLE.TEST";
        first.UserName = "a@example.test";
        first.NormalizedUserName = "A@EXAMPLE.TEST";

        var second =
            TestData.User("b");

        second.Email = "b@example.test";
        second.NormalizedEmail = "B@EXAMPLE.TEST";
        second.UserName = "b@example.test";
        second.NormalizedUserName = "B@EXAMPLE.TEST";

        var role =
            new ApplicationRole("User")
            {
                Id = Guid.NewGuid(),
                NormalizedName = "USER"
            };

        Context.AddRange(
            first,
            second,
            role);

        Context.AddRange(
            new ApplicationUserRole
            {
                UserId = first.Id,
                RoleId = role.Id
            },
            new ApplicationUserRole
            {
                UserId = second.Id,
                RoleId = role.Id
            });

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new Application.Features.User.Search.PagedSearchCriteria(
                null,
                null,
                null,
                null,
                [
                    new SortCriterion<UserSortField>(
                        UserSortField.Email,
                        SortDirection.Descending)
                ]);

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            ["b@example.test", "a@example.test"],
            result.Results.Select(x => x.Email).ToArray());
    }


    [Fact]
    public async Task SearchAsync_WhenRoleDiffersOnlyByCaseAndAccent_FindsUser()
    {
        var user = await TestData.AddUserAsync(Context, "role-accent");
        var role = new ApplicationRole("Éditeur")
        {
            Id = Guid.NewGuid(),
            NormalizedName = "EDITEUR"
        };

        Context.Add(role);
        Context.Add(new ApplicationUserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var criteria = new Application.Features.User.Search.PagedSearchCriteria(
            null,
            "EDITEUR",
            null,
            null,
            null);

        var queries = new UserQueries(
            Context,
            NullLogger<UserQueries>.Instance);

        var result = await queries.SearchAsync(
            criteria,
            TestContext.Current.CancellationToken);

        Assert.Equal(user.Id, Assert.Single(result.Results).Id);
    }

    [Theory]
    [InlineData(UserSortField.Email)]
    [InlineData(UserSortField.LastName)]
    [InlineData(UserSortField.FirstName)]
    [InlineData(UserSortField.IsActivated)]
    public async Task SearchAsync_ForEverySupportedSortField_ExecutesSuccessfully(
        UserSortField sortField)
    {
        // Arrange

        var user =
            await TestData.AddUserAsync(
                Context,
                "sort");

        var role =
            new ApplicationRole("User")
            {
                Id = Guid.NewGuid(),
                NormalizedName = "USER"
            };

        Context.Add(role);

        Context.Add(
            new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

        await Context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var criteria =
            new Application.Features.User.Search.PagedSearchCriteria(
                null,
                null,
                null,
                null,
                [
                    new SortCriterion<UserSortField>(
                        sortField,
                        SortDirection.Ascending)
                ]);

        var queries =
            new UserQueries(
                Context,
                NullLogger<UserQueries>.Instance);

        // Act

        var result =
            await queries.SearchAsync(
                criteria,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Single(
            result.Results);
    }


}
