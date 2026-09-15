using BudgetManager.Application.Common;
using BudgetManager.Application.Exceptions;
using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Seed;

public sealed class SeederFailureTests
{
    [Fact]
    public async Task RoleSeeder_WhenRoleCreationFails_ThrowsWithIdentityError()
    {
        var roleManager = CreateRoleManager();
        roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(false);
        roleManager.CreateAsync(Arg.Any<ApplicationRole>()).Returns(
            IdentityResult.Failed(new IdentityError
            {
                Code = "role_error",
                Description = "Role creation failed"
            }));

        var seeder = new RoleSeeder(
            roleManager,
            NullLogger<RoleSeeder>.Instance);

        var action = () => seeder.SeedAsync(TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);

        Assert.Contains("Role creation failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UsersSeeder_WhenRoleAssignmentFails_ThrowsWithIdentityError()
    {
        var userManager = CreateUserManager();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "admin@example.test",
            Email = "admin@example.test",
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User"
        };

        userManager.FindByEmailAsync("admin@example.test").Returns(user);
        userManager
            .GetUsersInRoleAsync(ApplicationRoles.Administrator)
            .Returns(Array.Empty<ApplicationUser>());
        userManager.IsInRoleAsync(user, ApplicationRoles.Administrator).Returns(false);
        userManager.AddToRoleAsync(user, ApplicationRoles.Administrator).Returns(
            IdentityResult.Failed(new IdentityError
            {
                Code = "role_assignment_error",
                Description = "Role assignment failed"
            }));

        var options = Options.Create(
            new UsersSeedOptions
            {
                Enabled = true,
                Users = new Dictionary<string, UserSeedOptions>
                {
                    ["Admin"] = new()
                    {
                        Enabled = true,
                        Role = ApplicationRoles.Administrator,
                        Email = "admin@example.test",
                        Password = "ValidPassword!123",
                        FirstName = "Admin",
                        LastName = "User"
                    }
                }
            });

        var seeder = new UsersSeeder(
            userManager,
            options,
            NullLogger<UsersSeeder>.Instance);

        var action = () => seeder.SeedAsync(TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<UpdateException>(action);

        var innerException =
            Assert.IsType<InvalidOperationException>(
                exception.InnerException);

        Assert.Contains(
            "Role assignment failed",
            innerException.Message,
            StringComparison.Ordinal);
    }

    private static UserManager<ApplicationUser> CreateUserManager()
    {
        return Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(),
            Options.Create(new IdentityOptions()),
            Substitute.For<IPasswordHasher<ApplicationUser>>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            Substitute.For<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Substitute.For<IServiceProvider>(),
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    private static RoleManager<ApplicationRole> CreateRoleManager()
    {
        return Substitute.For<RoleManager<ApplicationRole>>(
            Substitute.For<IRoleStore<ApplicationRole>>(),
            Array.Empty<IRoleValidator<ApplicationRole>>(),
            Substitute.For<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<ApplicationRole>>.Instance);
    }
}
