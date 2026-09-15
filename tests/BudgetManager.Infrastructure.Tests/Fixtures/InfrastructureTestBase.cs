using BudgetManager.Infrastructure.Persistence;
using BudgetManager.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Fixtures;

public abstract class InfrastructureTestBase(SqlServerFixture fixture) : IAsyncLifetime
{
    private string _databaseName = string.Empty;

    internal ApplicationDbContext Context { get; private set; } = null!;
    internal TestCurrentUser CurrentUser { get; } = new(Guid.NewGuid());
    internal MutableTimeProvider TimeProvider { get; } =
        new(new DateTimeOffset(2026, 8, 10, 8, 0, 0, TimeSpan.Zero));

    protected string DatabaseConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        _databaseName = $"BudgetManagerTests_{Guid.NewGuid():N}";

        var builder = new SqlConnectionStringBuilder(fixture.ConnectionString)
        {
            InitialCatalog = _databaseName
        };

        DatabaseConnectionString = builder.ConnectionString;
        Context = CreateContext();

        await Context.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
            await Context.DisposeAsync();
        }
    }

    internal ApplicationDbContext CreateContext(
        TestCurrentUser? currentUser = null,
        MutableTimeProvider? timeProvider = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(DatabaseConnectionString)
            .Options;

        IInterceptor[] interceptors =
        [
            new AuditableInterceptor(
                currentUser ?? CurrentUser,
                timeProvider ?? TimeProvider)
        ];

        return new ApplicationDbContext(
            options,
            interceptors,
            NullLogger<ApplicationDbContext>.Instance);
    }
}
