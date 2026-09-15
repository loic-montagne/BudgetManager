using BudgetManager.Infrastructure.Persistence.Seed;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Seed;

[Collection(SqlServerCollection.Name)]
public sealed class DatabaseInitializerTests(SqlServerFixture fixture)
    : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task InitializeAsync_RunsSeedersInOrderAndPropagatesCancellationToken()
    {
        // Arrange

        var executionOrder =
            new List<int>();

        var second =
            new RecordingSeeder(
                20,
                executionOrder);

        var first =
            new RecordingSeeder(
                10,
                executionOrder);

        var initializer =
            new DatabaseInitializer(
                Context,
                [second, first],
                NullLogger<DatabaseInitializer>.Instance);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        await initializer.InitializeAsync(
            cancellationToken);

        // Assert

        Assert.Equal(
            [10, 20],
            executionOrder);

        Assert.Equal(
            cancellationToken,
            first.ReceivedCancellationToken);

        Assert.Equal(
            cancellationToken,
            second.ReceivedCancellationToken);

        Assert.Equal(
            1,
            first.ExecutionCount);

        Assert.Equal(
            1,
            second.ExecutionCount);
    }

    private sealed class RecordingSeeder(
        int order,
        ICollection<int> executionOrder)
        : IDataSeeder
    {
        public int Order { get; } = order;

        public int ExecutionCount { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task SeedAsync(
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            ReceivedCancellationToken = cancellationToken;
            executionOrder.Add(Order);

            return Task.CompletedTask;
        }
    }
}
