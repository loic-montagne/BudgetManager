using BudgetManager.Application.Common;
using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Infrastructure.Extensions;
using BudgetManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BudgetManager.Infrastructure.Persistence.Seed;

internal sealed class UsersSeeder(UserManager<ApplicationUser> userManager, IOptions<UsersSeedOptions> options, ILogger<UsersSeeder> logger) : IDataSeeder
{
    private readonly UsersSeedOptions _options = options.Value;
    
    public int Order => 20;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            logger.LogDebug("Users seeding is disabled.");
            return;
        }

        foreach (var (userCode, userOptions) in _options.Users)
        {
            await SeedUserAsync(userCode, userOptions, cancellationToken);
        }
    }

    private async Task SeedUserAsync(string userCode, UserSeedOptions options, CancellationToken cancellationToken)
    {
        if (!options.Enabled)
        {
            logger.LogDebug("User {UserCode} seeding is disabled.", userCode);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        options.EnsureIsConfigured();

        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(options.Email!);

        cancellationToken.ThrowIfCancellationRequested();

        if (string.Equals(options.Role, ApplicationRoles.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            var administrators = await userManager.GetUsersInRoleAsync(ApplicationRoles.Administrator);
            var anotherActivatedAdministrator = administrators.Any(x => x.EmailConfirmed && (user is null || x.Id != user.Id));
            if (anotherActivatedAdministrator)
            {
                logger.LogDebug("Another activated administrator already exists. User {UserCode} seeding is skipped.", userCode);
                return;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (user is null)
        {
            logger.LogInformation("Creating user {UserCode}.", userCode);

            user = new ApplicationUser
            {
                UserName = options.Email,
                Email = options.Email,
                EmailConfirmed = true,
                LastName = options.LastName!,
                FirstName = options.FirstName!,
            };

            var result = await userManager.CreateAsync(user, options.Password!);
            result.EnsureSucceeded($"Unable to create user {userCode}");
            logger.LogInformation("User {UserCode} created.", userCode);
        }
        else
        {
            logger.LogDebug("User {UserCode} already exists.", userCode);

            if (string.Equals(options.Role, ApplicationRoles.Administrator, StringComparison.OrdinalIgnoreCase) && !user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                var result = await userManager.UpdateAsync(user);
                result.EnsureSucceeded($"Unable to activate administrator user {userCode}");
                logger.LogInformation("Administrator user {UserCode} has been activated.", userCode);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!await userManager.IsInRoleAsync(user, options.Role!))
        {
            logger.LogDebug("{Role} role is not assigned to user {UserCode}.", options.Role, userCode);
            var result = await userManager.AddToRoleAsync(user, options.Role!);
            result.EnsureSucceeded($"Unable to assign {options.Role} role to user {userCode}");
            logger.LogDebug("{Role} role has been assigned to user {UserCode}.", options.Role, userCode);
        }
    }
}
