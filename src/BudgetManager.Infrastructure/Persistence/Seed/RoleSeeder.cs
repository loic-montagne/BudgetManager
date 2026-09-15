using BudgetManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BudgetManager.Infrastructure.Persistence.Seed;

internal sealed class RoleSeeder(RoleManager<ApplicationRole> roleManager, ILogger<RoleSeeder> logger) : IDataSeeder
{
    public int Order => 10;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in Application.Common.ApplicationRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogDebug("Role '{Role}' already exists.", roleName);
                continue;
            }

            var result = await roleManager.CreateAsync(new ApplicationRole(roleName));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unable to create role '{roleName}' : " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            logger.LogInformation("Role '{Role}' created.", roleName);
        }
    }
}
