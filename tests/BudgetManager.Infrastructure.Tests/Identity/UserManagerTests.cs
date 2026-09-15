using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Infrastructure.Persistence;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Identity;

[Collection(SqlServerCollection.Name)]
public sealed class UserManagerTests(SqlServerFixture fixture) : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task RoleExistsAsync_WhenRoleExists_ReturnsTrue()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.User);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        Assert.True(await manager.RoleExistsAsync(ApplicationRoles.User, TestContext.Current.CancellationToken));
        Assert.False(await manager.RoleExistsAsync("Missing", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExistsAndEmailExistsAsync_RespectIdentifierAndExclusion()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await CreateIdentityUserAsync(identity, "existing@example.test", "Existing", "User");
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        Assert.True(await manager.ExistsAsync(user.Id, TestContext.Current.CancellationToken));
        Assert.False(await manager.ExistsAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
        Assert.True(
            await manager.EmailExistsAsync(
                "existing@example.test",
                null,
                TestContext.Current.CancellationToken));
        Assert.False(
            await manager.EmailExistsAsync(
                "existing@example.test",
                user.Id,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task IsBudgetOwnerAsync_DistinguishesOwnerAndMember()
    {
        var owner = await TestData.AddUserAsync(Context, "owner-identity-manager");
        var member = await TestData.AddUserAsync(Context, "member-identity-manager");
        var budget = Budget.Create("Identity manager budget", owner.Id);
        budget.GrantAccess(member.Id, Permission.View, owner.Id);
        Context.Add(budget);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        Assert.True(await manager.IsBudgetOwnerAsync(owner.Id, TestContext.Current.CancellationToken));
        Assert.False(await manager.IsBudgetOwnerAsync(member.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task IsLastActivatedAdministratorAsync_WhenUserIsOnlyActivatedAdministrator_ReturnsTrue()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.Administrator);
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var administrator = await CreateIdentityUserAsync(identity, "only-admin@example.test", "Admin", "Only");
        administrator.EmailConfirmed = true;
        Assert.True((await identity.UpdateAsync(administrator)).Succeeded);
        Assert.True((await identity.AddToRoleAsync(administrator, ApplicationRoles.Administrator)).Succeeded);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        var result = await manager.IsLastActivatedAdministratorAsync(
            administrator.Id,
            TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task IsLastActivatedAdministratorAsync_WhenAnotherActivatedAdministratorExists_ReturnsFalse()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.Administrator);
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var first = await CreateIdentityUserAsync(identity, "first-admin@example.test", "Admin", "First");
        var second = await CreateIdentityUserAsync(identity, "second-admin@example.test", "Admin", "Second");
        first.EmailConfirmed = true;
        second.EmailConfirmed = true;
        Assert.True((await identity.UpdateAsync(first)).Succeeded);
        Assert.True((await identity.UpdateAsync(second)).Succeeded);
        Assert.True((await identity.AddToRoleAsync(first, ApplicationRoles.Administrator)).Succeeded);
        Assert.True((await identity.AddToRoleAsync(second, ApplicationRoles.Administrator)).Succeeded);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        var result = await manager.IsLastActivatedAdministratorAsync(
            first.Id,
            TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task IsLastActivatedAdministratorAsync_WhenOtherAdministratorIsNotActivated_ReturnsTrue()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.Administrator);
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var activated = await CreateIdentityUserAsync(identity, "active-admin@example.test", "Admin", "Active");
        var pending = await CreateIdentityUserAsync(identity, "pending-admin@example.test", "Admin", "Pending");
        activated.EmailConfirmed = true;
        Assert.True((await identity.UpdateAsync(activated)).Succeeded);
        Assert.True((await identity.AddToRoleAsync(activated, ApplicationRoles.Administrator)).Succeeded);
        Assert.True((await identity.AddToRoleAsync(pending, ApplicationRoles.Administrator)).Succeeded);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        var result = await manager.IsLastActivatedAdministratorAsync(
            activated.Id,
            TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_PersistsProfileEmailPhoneAndRoles()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.User);
        await EnsureRoleAsync(scope, ApplicationRoles.Administrator);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var data = Profile("Last", "First", "+33612345678");

        var id = await manager.CreateAsync(
            "new@example.test",
            data,
            null,
            "ValidPassword!123",
            [ApplicationRoles.User, ApplicationRoles.Administrator],
            TestContext.Current.CancellationToken);

        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await identity.FindByIdAsync(id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal("new@example.test", persisted.Email);
        Assert.Equal("new@example.test", persisted.UserName);
        Assert.Equal("Last", persisted.LastName);
        Assert.Equal("First", persisted.FirstName);
        Assert.Equal("+33612345678", persisted.PhoneNumber);
        Assert.Equal(SupportedCultures.French, persisted.PreferredCulture);
        Assert.Null(persisted.PreferredTheme);
        var roles = await identity.GetRolesAsync(persisted);
        Assert.Equal(2, roles.Count);
        Assert.Contains(ApplicationRoles.User, roles);
        Assert.Contains(ApplicationRoles.Administrator, roles);
    }

    [Fact]
    public async Task CreateAsync_WhenRoleDoesNotExist_RollsBackCreatedUser()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        var action = () => manager.CreateAsync(
            "rollback-create@example.test",
            Profile("Last", "First", null),
            null,
            "ValidPassword!123",
            ["MissingRole"],
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(action);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.ChangeTracker.Clear();
        Assert.False(
            await db
                .Set<ApplicationUser>()
                .AnyAsync(
                    x => x.Email == "rollback-create@example.test",
                    TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EmailExistsAsync_UsesConfiguredCaseAndAccentInsensitiveCollation()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),

                // UserName reste volontairement ASCII :
                // ce test porte sur la collation de Email,
                // pas sur la validation Identity du UserName.
                UserName = "eric-login@example.test",
                NormalizedUserName = "ERIC-LOGIN@EXAMPLE.TEST",

                Email = "Éric@example.test",
                NormalizedEmail = "ÉRIC@EXAMPLE.TEST",

                LastName = "Last",
                FirstName = "First"
            };

        context.Add(
            user);

        await context.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        // Act

        var result =
            await manager.EmailExistsAsync(
                "eric@example.test",
                null,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.True(
            result);
    }

    [Fact]
    public async Task CreateAsync_WhenPasswordIsInvalid_RollsBackUser()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.User);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        var action = () => manager.CreateAsync(
            "bad-password@example.test",
            Profile("Last", "First", null),
            null,
            "weak",
            [ApplicationRoles.User],
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UpdateException>(action);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.ChangeTracker.Clear();
        Assert.False(
            await db
                .Set<ApplicationUser>()
                .AnyAsync(
                    x => x.Email == "bad-password@example.test",
                    TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_WhenDataIsNull_ThrowsArgumentNullException()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => manager.CreateAsync(
                "x@example.test",
                null!,
                null,
                "ValidPassword!123",
                [ApplicationRoles.User],
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesProfilePhoneAndRoles()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.User);
        await EnsureRoleAsync(scope, ApplicationRoles.Administrator);
        var appManager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var id = await appManager.CreateAsync(
            "update@example.test",
            Profile("OldLast", "OldFirst", null),
            null,
            "ValidPassword!123",
            [ApplicationRoles.User],
            TestContext.Current.CancellationToken);

        await appManager.UpdateAsync(
            id,
            Profile("NewLast", "NewFirst", "+33612345678", SupportedCultures.English, SupportedThemes.Dark),
            [ApplicationRoles.Administrator],
            TestContext.Current.CancellationToken);

        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await identity.FindByIdAsync(id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal("update@example.test", persisted.Email);
        Assert.Equal("update@example.test", persisted.UserName);
        Assert.Equal("NewLast", persisted.LastName);
        Assert.Equal("NewFirst", persisted.FirstName);
        Assert.Equal("+33612345678", persisted.PhoneNumber);
        Assert.Equal(SupportedCultures.English, persisted.PreferredCulture);
        Assert.Equal(SupportedThemes.Dark, persisted.PreferredTheme);
        var roles = await identity.GetRolesAsync(persisted);
        Assert.Equal([ApplicationRoles.Administrator], roles);
    }

    [Fact]
    public async Task UpdateAsync_WhenRemovingAdministratorRoleFromLastActivatedAdministrator_ThrowsAndRollsBackProfileChanges()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.User);
        await EnsureRoleAsync(scope, ApplicationRoles.Administrator);
        var appManager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var id = await appManager.CreateAsync(
            "last-admin-update@example.test",
            Profile("OldLast", "OldFirst", null),
            null,
            "ValidPassword!123",
            [ApplicationRoles.Administrator],
            TestContext.Current.CancellationToken);
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var administrator = await identity.FindByIdAsync(id.ToString());
        Assert.NotNull(administrator);
        administrator.EmailConfirmed = true;
        Assert.True((await identity.UpdateAsync(administrator)).Succeeded);

        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => appManager.UpdateAsync(
                id,
                Profile("NewLast", "NewFirst", "+33612345678", SupportedCultures.English, SupportedThemes.Dark),
                [ApplicationRoles.User],
                TestContext.Current.CancellationToken));

        Assert.Contains(
            exception.ValidationErrors,
            x => x.ErrorCode == ErrorCodes.UserIsLastActivatedAdministrator);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyIdentity = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await verifyIdentity.FindByIdAsync(id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal("OldLast", persisted.LastName);
        Assert.Equal("OldFirst", persisted.FirstName);
        Assert.Null(persisted.PhoneNumber);
        Assert.Equal(SupportedCultures.French, persisted.PreferredCulture);
        Assert.Null(persisted.PreferredTheme);
        Assert.True(await verifyIdentity.IsInRoleAsync(persisted, ApplicationRoles.Administrator));
        Assert.False(await verifyIdentity.IsInRoleAsync(persisted, ApplicationRoles.User));
    }

    [Fact]
    public async Task UpdateAsync_WhenAnotherActivatedAdministratorExists_AllowsRemovingAdministratorRole()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.User);
        await EnsureRoleAsync(scope, ApplicationRoles.Administrator);
        var appManager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var firstId = await appManager.CreateAsync(
            "admin-update-first@example.test",
            Profile("Admin", "First", null),
            null,
            "ValidPassword!123",
            [ApplicationRoles.Administrator],
            TestContext.Current.CancellationToken);
        var secondId = await appManager.CreateAsync(
            "admin-update-second@example.test",
            Profile("Admin", "Second", null),
            null,
            "ValidPassword!123",
            [ApplicationRoles.Administrator],
            TestContext.Current.CancellationToken);
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var id in new[] { firstId, secondId })
        {
            var administrator = await identity.FindByIdAsync(id.ToString());
            Assert.NotNull(administrator);
            administrator.EmailConfirmed = true;
            Assert.True((await identity.UpdateAsync(administrator)).Succeeded);
        }

        await appManager.UpdateAsync(
            firstId,
            Profile("Admin", "First", null),
            [ApplicationRoles.User],
            TestContext.Current.CancellationToken);

        var first = await identity.FindByIdAsync(firstId.ToString());
        var second = await identity.FindByIdAsync(secondId.ToString());
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.False(await identity.IsInRoleAsync(first, ApplicationRoles.Administrator));
        Assert.True(await identity.IsInRoleAsync(first, ApplicationRoles.User));
        Assert.True(await identity.IsInRoleAsync(second, ApplicationRoles.Administrator));
    }

    [Fact]
    public async Task UpdateAsync_WhenRolesDifferOnlyByCase_DoesNotLoseRole()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.User);
        var appManager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var id = await appManager.CreateAsync(
            "case-role@example.test",
            Profile("Last", "First", null),
            null,
            "ValidPassword!123",
            [ApplicationRoles.User],
            TestContext.Current.CancellationToken);

        await appManager.UpdateAsync(
            id,
            Profile("Last", "First", null),
            ["user"],
            TestContext.Current.CancellationToken);

        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await identity.FindByIdAsync(id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal([ApplicationRoles.User], await identity.GetRolesAsync(persisted));
    }

    [Fact]
    public async Task UpdateAsync_WhenRoleUpdateFails_RollsBackProfilePhoneAndPreferences()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.User);
        var appManager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var id = await appManager.CreateAsync(
            "rollback-update@example.test",
            Profile("OldLast", "OldFirst", null),
            null,
            "ValidPassword!123",
            [ApplicationRoles.User],
            TestContext.Current.CancellationToken);

        var action = () => appManager.UpdateAsync(
            id,
            Profile("NewLast", "NewFirst", "+33612345678", SupportedCultures.English, SupportedThemes.Dark),
            ["MissingRole"],
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(action);

        await using var verifyScope = provider.CreateAsyncScope();
        var identity = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await identity.FindByIdAsync(id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal("OldLast", persisted.LastName);
        Assert.Equal("OldFirst", persisted.FirstName);
        Assert.Null(persisted.PhoneNumber);
        Assert.Equal(SupportedCultures.French, persisted.PreferredCulture);
        Assert.Null(persisted.PreferredTheme);
        Assert.Equal([ApplicationRoles.User], await identity.GetRolesAsync(persisted));
    }

    [Fact]
    public async Task UpdateAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.UpdateAsync(
                Guid.NewGuid(),
                Profile(
                    "L",
                    "F",
                    null),
                [ApplicationRoles.User],
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_WhenDataIsNull_ThrowsArgumentNullException()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => manager.UpdateAsync(
                Guid.NewGuid(),
                null!,
                [ApplicationRoles.User],
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ActivateAsync_WhenValid_ConfirmsEmailReplacesTemporaryPasswordAndClearsActivationDates()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var identity =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user =
            await CreateIdentityUserAsync(
                identity,
                "activate@example.test",
                "Last",
                "First");

        var manager =
            scope.ServiceProvider.GetRequiredService<IUserManager>();

        var sentOn =
            new DateTimeOffset(
                2026,
                9,
                8,
                15,
                0,
                0,
                TimeSpan.Zero);

        var expiresOn =
            sentOn.AddDays(7);

        await manager.SaveActivationEmailAsync(
            user.Id,
            sentOn,
            expiresOn,
            TestContext.Current.CancellationToken);

        var token =
            await identity.GenerateEmailConfirmationTokenAsync(
                user);

        // Act

        await manager.ActivateAsync(
            user.Id,
            token,
            "NewPassword!123",
            TestContext.Current.CancellationToken);

        // Assert

        await using var verifyScope =
            provider.CreateAsyncScope();

        var verifyIdentity =
            verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var persisted =
            await verifyIdentity.FindByIdAsync(
                user.Id.ToString());

        Assert.NotNull(
            persisted);

        Assert.True(
            persisted.EmailConfirmed);

        Assert.Null(
            persisted.ActivationEmailSentOn);

        Assert.Null(
            persisted.ActivationEmailExpiresOn);

        Assert.False(
            await verifyIdentity.CheckPasswordAsync(
                persisted,
                "ValidPassword!123"));

        Assert.True(
            await verifyIdentity.CheckPasswordAsync(
                persisted,
                "NewPassword!123"));
    }

    [Fact]
    public async Task ActivateAsync_WhenActivationTokenIsInvalid_KeepsPendingAccountUnchanged()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var identity =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user =
            await CreateIdentityUserAsync(
                identity,
                "invalid-activation@example.test",
                "Last",
                "First");

        var manager =
            scope.ServiceProvider.GetRequiredService<IUserManager>();

        var sentOn =
            new DateTimeOffset(
                2026,
                9,
                8,
                15,
                0,
                0,
                TimeSpan.Zero);

        var expiresOn =
            sentOn.AddDays(7);

        await manager.SaveActivationEmailAsync(
            user.Id,
            sentOn,
            expiresOn,
            TestContext.Current.CancellationToken);

        // Act

        var action = () =>
            manager.ActivateAsync(
                user.Id,
                "invalid-token",
                "NewPassword!123",
                TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<UpdateException>(
            action);

        await using var verifyScope =
            provider.CreateAsyncScope();

        var verifyIdentity =
            verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var persisted =
            await verifyIdentity.FindByIdAsync(
                user.Id.ToString());

        Assert.NotNull(
            persisted);

        Assert.False(
            persisted.EmailConfirmed);

        Assert.Equal(
            sentOn,
            persisted.ActivationEmailSentOn);

        Assert.Equal(
            expiresOn,
            persisted.ActivationEmailExpiresOn);

        Assert.True(
            await verifyIdentity.CheckPasswordAsync(
                persisted,
                "ValidPassword!123"));
    }

    [Fact]
    public async Task ActivateAsync_WhenNewPasswordIsInvalid_RollsBackEmailConfirmationAndTemporaryPassword()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var identity =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user =
            await CreateIdentityUserAsync(
                identity,
                "rollback-activation@example.test",
                "Last",
                "First");

        var manager =
            scope.ServiceProvider.GetRequiredService<IUserManager>();

        var sentOn =
            new DateTimeOffset(
                2026,
                9,
                8,
                15,
                0,
                0,
                TimeSpan.Zero);

        var expiresOn =
            sentOn.AddDays(7);

        await manager.SaveActivationEmailAsync(
            user.Id,
            sentOn,
            expiresOn,
            TestContext.Current.CancellationToken);

        var token =
            await identity.GenerateEmailConfirmationTokenAsync(
                user);

        // Act

        var action = () =>
            manager.ActivateAsync(
                user.Id,
                token,
                "weak",
                TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<UpdateException>(
            action);

        await using var verifyScope =
            provider.CreateAsyncScope();

        var verifyIdentity =
            verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var persisted =
            await verifyIdentity.FindByIdAsync(
                user.Id.ToString());

        Assert.NotNull(
            persisted);

        Assert.False(
            persisted.EmailConfirmed);

        Assert.Equal(
            sentOn,
            persisted.ActivationEmailSentOn);

        Assert.Equal(
            expiresOn,
            persisted.ActivationEmailExpiresOn);

        Assert.True(
            await verifyIdentity.CheckPasswordAsync(
                persisted,
                "ValidPassword!123"));
    }

    [Fact]
    public async Task ActivateAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange

        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var action = () => manager.ActivateAsync(
            Guid.NewGuid(),
            "activation-token",
            "NewPassword!123",
            cancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(action);
    }

    [Fact]
    public async Task SaveActivationEmailAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange

        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var sentOn = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);

        var action = () => manager.SaveActivationEmailAsync(
            Guid.NewGuid(),
            sentOn,
            sentOn.AddDays(7),
            cancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(action);
    }

    [Fact]
    public async Task ChangeEmailAsync_WhenValid_ReturnsTokenWithoutChangingUser()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await CreateIdentityUserAsync(identity, "old@example.test", "Last", "First");
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        var token = await manager.ChangeEmailAsync(
            user.Id,
            "new@example.test",
            TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(token));
        var persisted = await identity.FindByIdAsync(user.Id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal("old@example.test", persisted.Email);
        Assert.Equal("old@example.test", persisted.UserName);
    }

    [Fact]
    public async Task ChangeEmailAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.ChangeEmailAsync(
                Guid.NewGuid(),
                "new@example.test",
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConfirmEmailChangeAsync_WhenValid_UpdatesEmailAndUserName()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await CreateIdentityUserAsync(identity, "confirm-old@example.test", "Last", "First");
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var token = await manager.ChangeEmailAsync(
            user.Id,
            "confirm-new@example.test",
            TestContext.Current.CancellationToken);

        await manager.ConfirmEmailChangeAsync(
            user.Id,
            "confirm-new@example.test",
            token,
            TestContext.Current.CancellationToken);

        var persisted = await identity.FindByIdAsync(user.Id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal("confirm-new@example.test", persisted.Email);
        Assert.Equal("confirm-new@example.test", persisted.UserName);
    }

    [Fact]
    public async Task ConfirmEmailChangeAsync_WhenUserNameUpdateFails_RollsBackEmailChange()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var source = await CreateIdentityUserAsync(identity, "confirm-source@example.test", "Source", "User");
        var blocker = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "taken@example.test",
            Email = "different@example.test",
            LastName = "Blocker",
            FirstName = "User"
        };
        var createBlocker = await identity.CreateAsync(blocker, "ValidPassword!123");
        Assert.True(createBlocker.Succeeded);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var token = await manager.ChangeEmailAsync(
            source.Id,
            "taken@example.test",
            TestContext.Current.CancellationToken);

        var action =
            () => manager.ConfirmEmailChangeAsync(
                source.Id,
                "taken@example.test",
                token,
                TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<Exception>(action);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyIdentity = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await verifyIdentity.FindByIdAsync(source.Id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal("confirm-source@example.test", persisted.Email);
        Assert.Equal("confirm-source@example.test", persisted.UserName);
    }

    [Fact]
    public async Task ConfirmEmailChangeAsync_WhenTokenIsInvalid_DoesNotChangeUser()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await CreateIdentityUserAsync(identity, "invalid-token@example.test", "Last", "First");
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        await Assert.ThrowsAnyAsync<Exception>(
            () => manager.ConfirmEmailChangeAsync(
                user.Id,
                "new-invalid-token@example.test",
                "invalid-token",
                TestContext.Current.CancellationToken));

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyIdentity = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await verifyIdentity.FindByIdAsync(user.Id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal("invalid-token@example.test", persisted.Email);
        Assert.Equal("invalid-token@example.test", persisted.UserName);
    }

    [Fact]
    public async Task ConfirmEmailChangeAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.ConfirmEmailChangeAsync(
                Guid.NewGuid(),
                "new@example.test",
                "token",
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenValid_UpdatesProfileWithoutChangingEmailOrRoles()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.User);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var id = await manager.CreateAsync(
            "profile@example.test",
            Profile("OldLast", "OldFirst", null),
            null,
            "ValidPassword!123",
            [ApplicationRoles.User],
            TestContext.Current.CancellationToken);

        await manager.UpdateProfileAsync(
            id,
            Profile(
                "NewLast",
                "NewFirst",
                "+33612345678",
                SupportedCultures.English,
                SupportedThemes.Dark),
            TestContext.Current.CancellationToken);

        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await identity.FindByIdAsync(id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal("profile@example.test", persisted.Email);
        Assert.Equal("profile@example.test", persisted.UserName);
        Assert.Equal("NewLast", persisted.LastName);
        Assert.Equal("NewFirst", persisted.FirstName);
        Assert.Equal("+33612345678", persisted.PhoneNumber);
        Assert.Equal(SupportedCultures.English, persisted.PreferredCulture);
        Assert.Equal(SupportedThemes.Dark, persisted.PreferredTheme);
        Assert.Equal([ApplicationRoles.User], await identity.GetRolesAsync(persisted));
    }


    [Fact]
    public async Task UpdateUiPreferencesAsync_WhenValid_UpdatesOnlyUiPreferences()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.User);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var id = await manager.CreateAsync(
            "ui-preferences@example.test",
            Profile("Last", "First", "+33612345678"),
            null,
            "ValidPassword!123",
            [ApplicationRoles.User],
            TestContext.Current.CancellationToken);

        await manager.UpdateUiPreferencesAsync(
            id,
            SupportedCultures.English,
            SupportedThemes.Dark,
            TestContext.Current.CancellationToken);

        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await identity.FindByIdAsync(id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal("ui-preferences@example.test", persisted.Email);
        Assert.Equal("ui-preferences@example.test", persisted.UserName);
        Assert.Equal("Last", persisted.LastName);
        Assert.Equal("First", persisted.FirstName);
        Assert.Equal("+33612345678", persisted.PhoneNumber);
        Assert.Equal(SupportedCultures.English, persisted.PreferredCulture);
        Assert.Equal(SupportedThemes.Dark, persisted.PreferredTheme);
        Assert.Equal([ApplicationRoles.User], await identity.GetRolesAsync(persisted));
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenPersistenceFails_RollsBackPhoneAndProfile()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await CreateIdentityUserAsync(identity, "rollback-profile@example.test", "OldLast", "OldFirst");

        var action =
            () => manager.UpdateProfileAsync(
                user.Id,
                Profile(
                    new string(
                        'L',
                        BudgetManager.Domain.Common.StringPropertyLengths.LastNameLength + 1),
                    "NewFirst",
                    "+33612345678",
                    SupportedCultures.English,
                    SupportedThemes.Dark),
                TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<Exception>(action);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyIdentity = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await verifyIdentity.FindByIdAsync(user.Id.ToString());
        Assert.NotNull(persisted);
        Assert.Equal("OldLast", persisted.LastName);
        Assert.Equal("OldFirst", persisted.FirstName);
        Assert.Null(persisted.PhoneNumber);
        Assert.Equal(SupportedCultures.French, persisted.PreferredCulture);
        Assert.Null(persisted.PreferredTheme);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.UpdateProfileAsync(
                Guid.NewGuid(),
                Profile("Last", "First", null),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenDataIsNull_ThrowsArgumentNullException()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => manager.UpdateProfileAsync(
                Guid.NewGuid(),
                null!,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_WhenUserHasOnlyNonOwnerAccesses_RemovesAccessesAndUser()
    {
        var owner = await TestData.AddUserAsync(Context, "delete-owner");
        var target = await TestData.AddUserAsync(Context, "delete-target");
        var budget = Budget.Create("Delete member budget", owner.Id);
        budget.GrantAccess(target.Id, Permission.View, owner.Id);
        Context.Add(budget);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.Administrator);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        await manager.DeleteAsync(target.Id, TestContext.Current.CancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.ChangeTracker.Clear();
        Assert.False(
            await db
                .Set<ApplicationUser>()
                .AnyAsync(
                    x => x.Id == target.Id,
                    TestContext.Current.CancellationToken));
        Assert.False(
            await db
                .Set<BudgetAccess>()
                .AnyAsync(
                    x => x.UserId == target.Id,
                    TestContext.Current.CancellationToken));
        Assert.True(await db.Set<Budget>().AnyAsync(x => x.Id == budget.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_WhenUserOwnsBudget_RollsBackRemovalOfOtherAccesses()
    {
        var owner = await TestData.AddUserAsync(Context, "protected-owner");
        var otherOwner = await TestData.AddUserAsync(Context, "other-owner");
        var ownedBudget = Budget.Create("Owned budget", owner.Id);
        var memberBudget = Budget.Create("Member budget", otherOwner.Id);
        memberBudget.GrantAccess(owner.Id, Permission.View, otherOwner.Id);
        Context.AddRange(ownedBudget, memberBudget);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.Administrator);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        await Assert.ThrowsAnyAsync<Exception>(
            () => manager.DeleteAsync(owner.Id, TestContext.Current.CancellationToken));

        await using var verifyScope = provider.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(
            await db
                .Set<ApplicationUser>()
                .AnyAsync(
                    x => x.Id == owner.Id,
                    TestContext.Current.CancellationToken));
        Assert.True(
            await db
                .Set<BudgetAccess>()
                .AnyAsync(
                    x =>
                        x.UserId == owner.Id
                        && x.BudgetId == memberBudget.Id,
                    TestContext.Current.CancellationToken));
        Assert.True(
            await db
                .Set<BudgetAccess>()
                .AnyAsync(
                    x =>
                        x.UserId == owner.Id
                        && x.BudgetId == ownedBudget.Id
                        && x.IsOwner,
                    TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsLastActivatedAdministrator_ThrowsAndKeepsUser()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.Administrator);
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var administrator = await CreateIdentityUserAsync(identity, "last-admin-delete@example.test", "Admin", "Last");
        administrator.EmailConfirmed = true;
        Assert.True((await identity.UpdateAsync(administrator)).Succeeded);
        Assert.True((await identity.AddToRoleAsync(administrator, ApplicationRoles.Administrator)).Succeeded);
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => manager.DeleteAsync(administrator.Id, TestContext.Current.CancellationToken));

        Assert.Contains(
            exception.ValidationErrors,
            x => x.ErrorCode == ErrorCodes.UserIsLastActivatedAdministrator);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyIdentity = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await verifyIdentity.FindByIdAsync(administrator.Id.ToString());
        Assert.NotNull(persisted);
        Assert.True(await verifyIdentity.IsInRoleAsync(persisted, ApplicationRoles.Administrator));
    }

    [Fact]
    public async Task DeleteAsync_WhenAnotherActivatedAdministratorExists_AllowsDeletion()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        await EnsureRoleAsync(scope, ApplicationRoles.Administrator);
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var first = await CreateIdentityUserAsync(identity, "delete-admin-first@example.test", "Admin", "First");
        var second = await CreateIdentityUserAsync(identity, "delete-admin-second@example.test", "Admin", "Second");
        foreach (var administrator in new[] { first, second })
        {
            administrator.EmailConfirmed = true;
            Assert.True((await identity.UpdateAsync(administrator)).Succeeded);
            Assert.True((await identity.AddToRoleAsync(administrator, ApplicationRoles.Administrator)).Succeeded);
        }
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();

        await manager.DeleteAsync(first.Id, TestContext.Current.CancellationToken);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyIdentity = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await verifyIdentity.FindByIdAsync(first.Id.ToString()));
        var remaining = await verifyIdentity.FindByIdAsync(second.Id.ToString());
        Assert.NotNull(remaining);
        Assert.True(await verifyIdentity.IsInRoleAsync(remaining, ApplicationRoles.Administrator));
    }

    [Fact]
    public async Task DeleteAsync_WhenTwoActivatedAdministratorsAreDeletedConcurrently_KeepsOneActivatedAdministrator()
    {
        await using var provider = CreateProvider();

        Guid firstId;
        Guid secondId;

        await using (var setupScope = provider.CreateAsyncScope())
        {
            await EnsureRoleAsync(setupScope, ApplicationRoles.Administrator);
            var identity = setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var first = await CreateIdentityUserAsync(identity, "concurrent-admin-first@example.test", "Admin", "First");
            var second = await CreateIdentityUserAsync(identity, "concurrent-admin-second@example.test", "Admin", "Second");
            foreach (var administrator in new[] { first, second })
            {
                administrator.EmailConfirmed = true;
                Assert.True((await identity.UpdateAsync(administrator)).Succeeded);
                Assert.True((await identity.AddToRoleAsync(administrator, ApplicationRoles.Administrator)).Succeeded);
            }

            firstId = first.Id;
            secondId = second.Id;
        }

        async Task<Exception?> TryDeleteAsync(Guid id)
        {
            try
            {
                await using var scope = provider.CreateAsyncScope();
                var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
                await manager.DeleteAsync(id, TestContext.Current.CancellationToken);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        var results = await Task.WhenAll(
            TryDeleteAsync(firstId),
            TryDeleteAsync(secondId));

        Assert.Equal(1, results.Count(x => x is null));

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyIdentity = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var activatedAdministrators = await verifyIdentity.GetUsersInRoleAsync(ApplicationRoles.Administrator);
        Assert.Single(activatedAdministrators.Where(x => x.EmailConfirmed));
    }

    [Fact]
    public async Task DeleteAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IUserManager>();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
    }


    [Fact]
    public async Task ProfilePictureExistsAsync_WhenPictureExists_ReturnsTrue()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var identity =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var user =
            await CreateIdentityUserAsync(
                identity,
                "picture-exists@example.test",
                "Last",
                "First");

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        context.Add(
            new ApplicationUserProfilePicture
            {
                UserId = user.Id,
                Content = [1, 2, 3],
                ContentType = SupportedPictureFormats.WebpContentType
            });

        var cancellationToken =
            TestContext.Current.CancellationToken;

        await context.SaveChangesAsync(
            cancellationToken);

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        // Act

        var result =
            await manager.ProfilePictureExistsAsync(
                user.Id,
                cancellationToken);

        // Assert

        Assert.True(
            result);
    }

    [Fact]
    public async Task ProfilePictureExistsAsync_WhenPictureDoesNotExist_ReturnsFalse()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var identity =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var user =
            await CreateIdentityUserAsync(
                identity,
                "picture-not-exists@example.test",
                "Last",
                "First");

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            await manager.ProfilePictureExistsAsync(
                user.Id,
                cancellationToken);

        // Assert

        Assert.False(
            result);
    }

    [Fact]
    public async Task CreateAsync_WhenProfilePictureProvided_PersistsPicture()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        await EnsureRoleAsync(
            scope,
            ApplicationRoles.User);

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        var picture =
            new UserProfilePicture(
                [1, 2, 3, 4],
                SupportedPictureFormats.WebpContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var id =
            await manager.CreateAsync(
                "picture-create@example.test",
                Profile(
                    "Last",
                    "First",
                    null,
                    SupportedCultures.English,
                    SupportedThemes.System),
                picture,
                "ValidPassword!123",
                [ApplicationRoles.User],
                cancellationToken);

        // Assert

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var persisted =
            await context
                .Set<ApplicationUserProfilePicture>()
                .SingleAsync(
                    x => x.UserId == id,
                    cancellationToken);

        Assert.Equal(
            picture.Content,
            persisted.Content);

        Assert.Equal(
            picture.ContentType,
            persisted.ContentType);
    }

    [Fact]
    public async Task CreateAsync_WhenRoleAssignmentFailsWithProfilePicture_RollsBackUserAndPicture()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        var picture =
            new UserProfilePicture(
                [1, 2, 3],
                SupportedPictureFormats.WebpContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var action = () =>
            manager.CreateAsync(
                "picture-rollback@example.test",
                Profile(
                    "Last",
                    "First",
                    null),
                picture,
                "ValidPassword!123",
                ["MissingRole"],
                cancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(
            action);

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var pictureUserId =
            context.ChangeTracker
                .Entries<ApplicationUserProfilePicture>()
                .Select(x => x.Entity.UserId)
                .Single();

        context.ChangeTracker.Clear();

        Assert.False(
            await context
                .Set<ApplicationUser>()
                .AnyAsync(
                    x => x.Email == "picture-rollback@example.test",
                    cancellationToken));

        Assert.False(
            await context
                .Set<ApplicationUserProfilePicture>()
                .AnyAsync(
                    x => x.UserId == pictureUserId,
                    cancellationToken));
    }

    [Fact]
    public async Task UpdateProfilePictureAsync_WhenPictureDoesNotExist_CreatesPicture()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var identity =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var user =
            await CreateIdentityUserAsync(
                identity,
                "picture-add@example.test",
                "Last",
                "First");

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        var picture =
            new UserProfilePicture(
                [10, 11],
                SupportedPictureFormats.WebpContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        await manager.UpdateProfilePictureAsync(
            user.Id,
            picture,
            cancellationToken);

        // Assert

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        context.ChangeTracker.Clear();

        var persisted =
            await context
                .Set<ApplicationUserProfilePicture>()
                .SingleAsync(
                    x => x.UserId == user.Id,
                    cancellationToken);

        Assert.Equal(
            picture.Content,
            persisted.Content);

        Assert.Equal(
            picture.ContentType,
            persisted.ContentType);
    }

    [Fact]
    public async Task UpdateProfilePictureAsync_WhenPictureExists_ReplacesPicture()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var identity =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var user =
            await CreateIdentityUserAsync(
                identity,
                "picture-replace@example.test",
                "Last",
                "First");

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        await manager.UpdateProfilePictureAsync(
            user.Id,
            new UserProfilePicture(
                [1],
                SupportedPictureFormats.PngContentType),
            cancellationToken);

        var replacement =
            new UserProfilePicture(
                [8, 9],
                SupportedPictureFormats.WebpContentType);

        // Act

        await manager.UpdateProfilePictureAsync(
            user.Id,
            replacement,
            cancellationToken);

        // Assert

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        context.ChangeTracker.Clear();

        var persisted =
            await context
                .Set<ApplicationUserProfilePicture>()
                .SingleAsync(
                    x => x.UserId == user.Id,
                    cancellationToken);

        Assert.Equal(
            replacement.Content,
            persisted.Content);

        Assert.Equal(
            replacement.ContentType,
            persisted.ContentType);
    }

    [Fact]
    public async Task UpdateProfilePictureAsync_WhenPictureIsNull_ThrowsArgumentNullException()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var action = () =>
            manager.UpdateProfilePictureAsync(
                Guid.NewGuid(),
                null!,
                cancellationToken);

        // Assert

        await Assert.ThrowsAsync<ArgumentNullException>(
            action);
    }

    [Fact]
    public async Task UpdateProfilePictureAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var action = () =>
            manager.UpdateProfilePictureAsync(
                Guid.NewGuid(),
                new UserProfilePicture(
                    [1],
                    SupportedPictureFormats.WebpContentType),
                cancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(
            action);
    }

    [Fact]
    public async Task DeleteProfilePictureAsync_WhenPictureExists_RemovesPicture()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var identity =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var user =
            await CreateIdentityUserAsync(
                identity,
                "picture-delete@example.test",
                "Last",
                "First");

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        await manager.UpdateProfilePictureAsync(
            user.Id,
            new UserProfilePicture(
                [1, 2],
                SupportedPictureFormats.WebpContentType),
            cancellationToken);

        // Act

        await manager.DeleteProfilePictureAsync(
            user.Id,
            cancellationToken);

        // Assert

        Assert.False(
            await manager.ProfilePictureExistsAsync(
                user.Id,
                cancellationToken));
    }

    [Fact]
    public async Task DeleteProfilePictureAsync_WhenPictureDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var identity =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var user =
            await CreateIdentityUserAsync(
                identity,
                "picture-missing@example.test",
                "Last",
                "First");

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var action = () =>
            manager.DeleteProfilePictureAsync(
                user.Id,
                cancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(
            action);
    }

    [Fact]
    public async Task DeleteProfilePictureAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var action = () =>
            manager.DeleteProfilePictureAsync(
                Guid.NewGuid(),
                cancellationToken);

        // Assert

        await Assert.ThrowsAsync<InvalidOperationException>(
            action);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserHasProfilePicture_RemovesPictureByCascade()
    {
        // Arrange

        await using var provider =
            CreateProvider();

        await using var scope =
            provider.CreateAsyncScope();

        await EnsureRoleAsync(
            scope,
            ApplicationRoles.Administrator);

        var identity =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var user =
            await CreateIdentityUserAsync(
                identity,
                "picture-cascade@example.test",
                "Last",
                "First");

        var manager =
            scope.ServiceProvider
                .GetRequiredService<IUserManager>();

        var cancellationToken =
            TestContext.Current.CancellationToken;

        await manager.UpdateProfilePictureAsync(
            user.Id,
            new UserProfilePicture(
                [1, 2, 3],
                SupportedPictureFormats.WebpContentType),
            cancellationToken);

        // Act

        await manager.DeleteAsync(
            user.Id,
            cancellationToken);

        // Assert

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        context.ChangeTracker.Clear();

        Assert.False(
            await context
                .Set<ApplicationUserProfilePicture>()
                .AnyAsync(
                    x => x.UserId == user.Id,
                    cancellationToken));
    }

    [Fact]
    public async Task PasswordValidator_WhenPasswordIsStrong_ReturnsNoErrors()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var validator =
            scope.ServiceProvider
                .GetRequiredService<
                    BudgetManager.Application.Abstractions.Identity.IPasswordValidator>();
        var errors = await validator.ValidateAsync(
            "password@example.test",
            Profile("Last", "First", null),
            "ValidPassword!123",
            TestContext.Current.CancellationToken);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public async Task PasswordValidator_WhenPasswordIsWeak_ReturnsIdentityErrors()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var validator =
            scope.ServiceProvider
                .GetRequiredService<
                    BudgetManager.Application.Abstractions.Identity.IPasswordValidator>();
        var errors = await validator.ValidateAsync(
            "password@example.test",
            Profile("Last", "First", null),
            "weak",
            TestContext.Current.CancellationToken);
        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, x => x.Code == "PasswordTooShort");
    }

    private static UserProfileData Profile(
        string lastName,
        string firstName,
        string? phoneNumber,
        string preferredCulture = SupportedCultures.French,
        string? preferredTheme = null)
        => new(
            lastName,
            firstName,
            phoneNumber,
            preferredCulture,
            preferredTheme);

    private ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(DatabaseConnectionString));
        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 4;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<IUserManager, BudgetManager.Infrastructure.Identity.UserManager>();
        services.AddScoped<
            BudgetManager.Application.Abstractions.Identity.IPasswordValidator,
            BudgetManager.Infrastructure.Identity.PasswordValidator>();
        return services.BuildServiceProvider();
    }

    private static async Task EnsureRoleAsync(AsyncServiceScope scope, string role)
    {
        var manager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        if (!await manager.RoleExistsAsync(role))
        {
            var result = await manager.CreateAsync(new ApplicationRole(role));
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(x => x.Description)));
        }
    }

    private static async Task<ApplicationUser> CreateIdentityUserAsync(
        UserManager<ApplicationUser> manager,
        string email,
        string lastName,
        string firstName)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            LastName = lastName,
            FirstName = firstName
        };
        var result = await manager.CreateAsync(user, "ValidPassword!123");
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(x => x.Description)));
        return user;
    }
}
