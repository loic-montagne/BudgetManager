using BudgetManager.Domain.Entities.Common;

namespace BudgetManager.Application.Abstractions.Persistence.Common;

/// <summary>
/// Extends an entity repository with case-specific name uniqueness checks.
/// </summary>
public interface INamedEntityRepository<TEntity> : IEntityRepository<TEntity> where TEntity : Entity
{
    Task<bool> IsNameUniqueAsync(string name, Guid? excludingId, CancellationToken cancellationToken);
}
