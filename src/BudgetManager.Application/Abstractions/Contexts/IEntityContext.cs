namespace BudgetManager.Application.Abstractions.Contexts;

/// <summary>
/// Provides scoped access to tracked domain entities and reuses entities already loaded during the current use case.
/// </summary>
public interface IEntityContext<TEntity> where TEntity : Domain.Entities.Common.Entity
{
    /// <summary>
    /// Gets the tracked entity, or <see langword="null"/> when it does not exist.
    /// </summary>
    Task<TEntity?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the tracked entity.
    /// </summary>
    /// <exception cref="Exceptions.NotFoundException{TEntity}">The entity does not exist.</exception>
    Task<TEntity> GetRequiredAsync(Guid id, CancellationToken cancellationToken);
}
