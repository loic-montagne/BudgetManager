using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BudgetManager.Infrastructure.Persistence.Seed;

internal sealed class DatabaseInitializer(ApplicationDbContext context, IEnumerable<IDataSeeder> seeders, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Database initialization started.");
        await ApplyMigrationsAsync(cancellationToken);
        logger.LogInformation("Database migrations application completed.");
        await RunSeedersAsync(cancellationToken);
        logger.LogInformation("Database seed completed.");
        logger.LogInformation("Database initialization completed.");
    }

    private async Task ApplyMigrationsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying migrations...");
        await context.Database.MigrateAsync(cancellationToken);
    }

    private async Task RunSeedersAsync(CancellationToken cancellationToken)
    {
        foreach (var seeder in seeders.OrderBy(x => x.Order))
        {
            logger.LogInformation("Running seeder {Seeder}.",
                seeder.GetType().Name);

            await seeder.SeedAsync(cancellationToken);
        }
    }
}
