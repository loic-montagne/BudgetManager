using BudgetManager.Infrastructure;
using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Common;
using BudgetManager.Application.Exceptions;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Infrastructure.Persistence.Seed;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Seed;

[Collection(SqlServerCollection.Name)]
public sealed class SeedingIntegrationTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task InitializeAsync_WhenUserSeedingIsEnabled_CreatesRolesAndConfiguredUsers()
    {
        // Arrange

        await using var provider =
            CreateProvider(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "true",
                    ["Seed:Users:Users:BootstrapAdmin:Enabled"] = "true",
                    ["Seed:Users:Users:BootstrapAdmin:Role"] = ApplicationRoles.Administrator,
                    ["Seed:Users:Users:BootstrapAdmin:Email"] = "admin@example.test",
                    ["Seed:Users:Users:BootstrapAdmin:Password"] = "ValidPassword!123",
                    ["Seed:Users:Users:BootstrapAdmin:LastName"] = "Admin",
                    ["Seed:Users:Users:BootstrapAdmin:FirstName"] = "Bootstrap",
                    ["Seed:Users:Users:StandardUser:Enabled"] = "true",
                    ["Seed:Users:Users:StandardUser:Role"] = ApplicationRoles.User,
                    ["Seed:Users:Users:StandardUser:Email"] = "user@example.test",
                    ["Seed:Users:Users:StandardUser:Password"] = "AnotherPassword!456",
                    ["Seed:Users:Users:StandardUser:LastName"] = "User",
                    ["Seed:Users:Users:StandardUser:FirstName"] = "Standard"
                });

        using var scope =
            provider.CreateScope();

        var initializer =
            scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        // Act

        await initializer.InitializeAsync(
            TestContext.Current.CancellationToken);

        // Assert

        var roleManager =
            scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in ApplicationRoles.All)
        {
            Assert.True(
                await roleManager.RoleExistsAsync(
                    role));
        }

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin =
            await userManager.FindByEmailAsync(
                "admin@example.test");

        Assert.NotNull(admin);

        Assert.True(
            await userManager.IsInRoleAsync(
                admin,
                ApplicationRoles.Administrator));

        Assert.Equal(
            "admin@example.test",
            admin.UserName);

        Assert.Equal(
            "Bootstrap",
            admin.FirstName);

        Assert.Equal(
            "Admin",
            admin.LastName);

        var user =
            await userManager.FindByEmailAsync(
                "user@example.test");

        Assert.NotNull(user);

        Assert.True(
            await userManager.IsInRoleAsync(
                user,
                ApplicationRoles.User));
    }

    [Fact]
    public async Task InitializeAsync_WhenExecutedTwice_IsIdempotent()
    {
        // Arrange

        await using var provider =
            CreateProvider(
                CreateSingleUserConfiguration());

        // Act

        using (var firstScope = provider.CreateScope())
        {
            var initializer =
                firstScope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

            await initializer.InitializeAsync(
                TestContext.Current.CancellationToken);
        }

        using (var secondScope = provider.CreateScope())
        {
            var initializer =
                secondScope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

            await initializer.InitializeAsync(
                TestContext.Current.CancellationToken);
        }

        // Assert

        using var assertionScope =
            provider.CreateScope();

        var userManager =
            assertionScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var users =
            userManager.Users
                .Where(x => x.Email == "admin@example.test")
                .ToList();

        Assert.Single(users);

        var roleManager =
            assertionScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        Assert.Equal(
            ApplicationRoles.All.Count,
            roleManager.Roles.Count());
    }

    [Fact]
    public async Task InitializeAsync_WhenUserAlreadyExistsWithoutRole_AssignsConfiguredRoleWithoutRecreatingUser()
    {
        // Arrange

        await using var provider =
            CreateProvider(
                CreateSingleUserConfiguration());

        Guid existingUserId;

        using (var setupScope = provider.CreateScope())
        {
            var userManager =
                setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var existing =
                new ApplicationUser
                {
                    UserName = "admin@example.test",
                    Email = "admin@example.test",
                    EmailConfirmed = true,
                    FirstName = "Existing",
                    LastName = "User"
                };

            var creation =
                await userManager.CreateAsync(
                    existing,
                    "ValidPassword!123");

            Assert.True(
                creation.Succeeded);

            existingUserId = existing.Id;
        }

        using var scope =
            provider.CreateScope();

        var initializer =
            scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        // Act

        await initializer.InitializeAsync(
            TestContext.Current.CancellationToken);

        // Assert

        var userManagerForAssertion =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user =
            await userManagerForAssertion.FindByEmailAsync(
                "admin@example.test");

        Assert.NotNull(user);

        Assert.Equal(
            existingUserId,
            user.Id);

        Assert.Equal(
            "Existing",
            user.FirstName);

        Assert.True(
            await userManagerForAssertion.IsInRoleAsync(
                user,
                ApplicationRoles.Administrator));
    }

    [Fact]
    public async Task InitializeAsync_WhenAnotherActivatedAdministratorAlreadyExists_DoesNotCreateConfiguredBootstrapAdministrator()
    {
        await using var provider = CreateProvider(CreateSingleUserConfiguration());

        using (var setupScope = provider.CreateScope())
        {
            var roleManager = setupScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            Assert.True((await roleManager.CreateAsync(new ApplicationRole(ApplicationRoles.Administrator))).Succeeded);

            var userManager = setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var existing = new ApplicationUser
            {
                UserName = "existing-admin@example.test",
                Email = "existing-admin@example.test",
                EmailConfirmed = true,
                FirstName = "Existing",
                LastName = "Admin"
            };
            Assert.True((await userManager.CreateAsync(existing, "ValidPassword!123")).Succeeded);
            Assert.True((await userManager.AddToRoleAsync(existing, ApplicationRoles.Administrator)).Succeeded);
        }

        using (var scope = provider.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await initializer.InitializeAsync(TestContext.Current.CancellationToken);
        }

        using var assertionScope = provider.CreateScope();
        var assertionUserManager = assertionScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await assertionUserManager.FindByEmailAsync("admin@example.test"));
        var existingAdministrator = await assertionUserManager.FindByEmailAsync("existing-admin@example.test");
        Assert.NotNull(existingAdministrator);
        Assert.True(await assertionUserManager.IsInRoleAsync(existingAdministrator, ApplicationRoles.Administrator));
    }

    [Fact]
    public async Task InitializeAsync_WhenOnlyOtherAdministratorIsNotActivated_CreatesConfiguredBootstrapAdministrator()
    {
        await using var provider = CreateProvider(CreateSingleUserConfiguration());

        using (var setupScope = provider.CreateScope())
        {
            var roleManager = setupScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            Assert.True((await roleManager.CreateAsync(new ApplicationRole(ApplicationRoles.Administrator))).Succeeded);

            var userManager = setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var pending = new ApplicationUser
            {
                UserName = "pending-admin@example.test",
                Email = "pending-admin@example.test",
                EmailConfirmed = false,
                FirstName = "Pending",
                LastName = "Admin"
            };
            Assert.True((await userManager.CreateAsync(pending, "ValidPassword!123")).Succeeded);
            Assert.True((await userManager.AddToRoleAsync(pending, ApplicationRoles.Administrator)).Succeeded);
        }

        using (var scope = provider.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await initializer.InitializeAsync(TestContext.Current.CancellationToken);
        }

        using var assertionScope = provider.CreateScope();
        var assertionUserManager = assertionScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var bootstrap = await assertionUserManager.FindByEmailAsync("admin@example.test");
        Assert.NotNull(bootstrap);
        Assert.True(bootstrap.EmailConfirmed);
        Assert.True(await assertionUserManager.IsInRoleAsync(bootstrap, ApplicationRoles.Administrator));
    }

    [Fact]
    public async Task InitializeAsync_WhenConfiguredAdministratorAlreadyExistsButIsNotActivated_ActivatesItWithoutRecreatingIt()
    {
        await using var provider = CreateProvider(CreateSingleUserConfiguration());
        Guid existingUserId;

        using (var setupScope = provider.CreateScope())
        {
            var userManager = setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var existing = new ApplicationUser
            {
                UserName = "admin@example.test",
                Email = "admin@example.test",
                EmailConfirmed = false,
                FirstName = "Existing",
                LastName = "Admin"
            };
            Assert.True((await userManager.CreateAsync(existing, "ValidPassword!123")).Succeeded);
            existingUserId = existing.Id;
        }

        using (var scope = provider.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await initializer.InitializeAsync(TestContext.Current.CancellationToken);
        }

        using var assertionScope = provider.CreateScope();
        var assertionUserManager = assertionScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var administrator = await assertionUserManager.FindByEmailAsync("admin@example.test");
        Assert.NotNull(administrator);
        Assert.Equal(existingUserId, administrator.Id);
        Assert.True(administrator.EmailConfirmed);
        Assert.True(await assertionUserManager.IsInRoleAsync(administrator, ApplicationRoles.Administrator));
    }

    [Fact]
    public async Task InitializeAsync_WhenIndividualUserIsDisabled_DoesNotCreateDisabledUser()
    {
        // Arrange

        var configuration =
            CreateSingleUserConfiguration();

        configuration["Seed:Users:Users:Disabled:Enabled"] = "false";
        configuration["Seed:Users:Users:Disabled:Role"] = ApplicationRoles.User;
        configuration["Seed:Users:Users:Disabled:Email"] = "disabled@example.test";
        configuration["Seed:Users:Users:Disabled:Password"] = "DisabledPassword!789";
        configuration["Seed:Users:Users:Disabled:LastName"] = "Disabled";
        configuration["Seed:Users:Users:Disabled:FirstName"] = "User";

        await using var provider =
            CreateProvider(
                configuration);

        using var scope =
            provider.CreateScope();

        var initializer =
            scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        // Act

        await initializer.InitializeAsync(
            TestContext.Current.CancellationToken);

        // Assert

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.Null(
            await userManager.FindByEmailAsync(
                "disabled@example.test"));
    }

    [Fact]
    public async Task InitializeAsync_WhenGlobalUserSeedingIsDisabled_DoesNotCreateUsers()
    {
        // Arrange

        await using var provider =
            CreateProvider(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false"
                });

        using var scope =
            provider.CreateScope();

        var initializer =
            scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        // Act

        await initializer.InitializeAsync(
            TestContext.Current.CancellationToken);

        // Assert

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.False(
            userManager.Users.Any());
    }


    [Fact]
    public async Task InitializeAsync_WhenTwoSeedEntriesUseSameEmail_CreatesSingleUserAndAssignsBothRoles()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Seed:Users:Enabled"] = "true",
            ["Seed:Users:Users:Admin:Enabled"] = "true",
            ["Seed:Users:Users:Admin:Role"] = ApplicationRoles.Administrator,
            ["Seed:Users:Users:Admin:Email"] = "shared@example.test",
            ["Seed:Users:Users:Admin:Password"] = "ValidPassword!123",
            ["Seed:Users:Users:Admin:LastName"] = "Shared",
            ["Seed:Users:Users:Admin:FirstName"] = "User",
            ["Seed:Users:Users:Standard:Enabled"] = "true",
            ["Seed:Users:Users:Standard:Role"] = ApplicationRoles.User,
            ["Seed:Users:Users:Standard:Email"] = "shared@example.test",
            ["Seed:Users:Users:Standard:Password"] = "AnotherPassword!456",
            ["Seed:Users:Users:Standard:LastName"] = "Ignored",
            ["Seed:Users:Users:Standard:FirstName"] = "Ignored"
        };

        await using var provider = CreateProvider(configuration);
        using var scope = provider.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        await initializer.InitializeAsync(TestContext.Current.CancellationToken);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var users = userManager.Users
            .Where(x => x.Email == "shared@example.test")
            .ToList();

        var user = Assert.Single(users);
        Assert.True(await userManager.IsInRoleAsync(user, ApplicationRoles.Administrator));
        Assert.True(await userManager.IsInRoleAsync(user, ApplicationRoles.User));
        Assert.Equal("Shared", user.LastName);
        Assert.Equal("User", user.FirstName);
    }

    [Fact]
    public async Task InitializeAsync_WhenConfiguredPasswordDoesNotMeetIdentityPolicy_Throws()
    {
        // Arrange

        await using var provider =
            CreateProvider(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "true",
                    ["Seed:Users:Users:Invalid:Enabled"] = "true",
                    ["Seed:Users:Users:Invalid:Role"] = ApplicationRoles.Administrator,
                    ["Seed:Users:Users:Invalid:Email"] = "invalid@example.test",
                    ["Seed:Users:Users:Invalid:Password"] = "weak",
                    ["Seed:Users:Users:Invalid:LastName"] = "Invalid",
                    ["Seed:Users:Users:Invalid:FirstName"] = "Password"
                });

        using var scope =
            provider.CreateScope();

        var initializer =
            scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        // Act

        var action = () => initializer.InitializeAsync(
            TestContext.Current.CancellationToken);

        // Assert

        var exception =
            await Assert.ThrowsAsync<UpdateException>(
                action);

        var innerException =
            Assert.IsType<InvalidOperationException>(
                exception.InnerException);

        Assert.Contains(
            "Unable to create user Invalid",
            innerException.Message,
            StringComparison.Ordinal);
    }


    private ServiceProvider CreateProvider(
        IDictionary<string, string?> values)
    {
        var configurationValues =
            new Dictionary<string, string?>(values)
            {
                ["ConnectionStrings:DefaultConnection"] =
                    DatabaseConnectionString,

                ["Database:CommandTimeout"] =
                    "30"
            };

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationValues)
                .Build();

        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddSingleton<ICurrentUser>(
            CurrentUser);

        services.AddSingleton<TimeProvider>(
            TimeProvider);

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        return services.BuildServiceProvider(
            validateScopes: true);
    }

    private static Dictionary<string, string?> CreateSingleUserConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["Seed:Users:Enabled"] = "true",
            ["Seed:Users:Users:BootstrapAdmin:Enabled"] = "true",
            ["Seed:Users:Users:BootstrapAdmin:Role"] = ApplicationRoles.Administrator,
            ["Seed:Users:Users:BootstrapAdmin:Email"] = "admin@example.test",
            ["Seed:Users:Users:BootstrapAdmin:Password"] = "ValidPassword!123",
            ["Seed:Users:Users:BootstrapAdmin:LastName"] = "Admin",
            ["Seed:Users:Users:BootstrapAdmin:FirstName"] = "Bootstrap"
        };
    }
}
