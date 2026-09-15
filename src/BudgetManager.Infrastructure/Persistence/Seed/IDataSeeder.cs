namespace BudgetManager.Infrastructure.Persistence.Seed
{
    public interface IDataSeeder
    {
        int Order { get; }
        Task SeedAsync(CancellationToken cancellationToken);
    }
}
