using BudgetManager.Domain.Entities.Common;

namespace BudgetManager.Application.Abstractions.Persistence.Common;

/// <summary>
/// Defines persistence operations for auditable entities.
/// </summary>
public interface IAuditableRepository<TEntity> : IGenericRepository<TEntity> where TEntity : Auditable
{
}
