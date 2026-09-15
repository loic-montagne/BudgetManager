using BudgetManager.Domain.Entities.Common;

namespace BudgetManager.Application.Abstractions.Persistence.Common;

/// <summary>
/// Defines command-side persistence operations for domain entities.
/// </summary>
public interface IEntityRepository<TEntity> : IAuditableRepository<TEntity> where TEntity : Entity
{
    /// <summary>
    /// Loads an entity tracked by the persistence context for a subsequent mutation.
    /// </summary>
    Task<TEntity?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);
}
