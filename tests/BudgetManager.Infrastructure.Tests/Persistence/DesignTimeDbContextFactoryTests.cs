using BudgetManager.Infrastructure.Persistence.Design;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Persistence;

public sealed class DesignTimeDbContextFactoryTests
{
    [Fact]
    public async Task CreateDbContext_ReturnsSqlServerContext()
    {
        // Arrange

        var factory =
            new ApplicationDbContextFactory();

        // Act

        await using var context =
            factory.CreateDbContext([]);

        // Assert

        Assert.Equal(
            "Microsoft.EntityFrameworkCore.SqlServer",
            context.Database.ProviderName);
    }
}
