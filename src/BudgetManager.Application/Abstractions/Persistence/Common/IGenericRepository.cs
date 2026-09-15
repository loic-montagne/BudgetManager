namespace BudgetManager.Application.Abstractions.Persistence.Common;

/// <summary>
/// Defines persistence operations for command-side entities.
/// </summary>
public interface IGenericRepository<TEntity> where TEntity : class
{
    Task CreateAsync(TEntity entity, CancellationToken cancellationToken);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken);

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken);
}
