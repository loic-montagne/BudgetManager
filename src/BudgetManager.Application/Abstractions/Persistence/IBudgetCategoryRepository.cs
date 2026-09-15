using BudgetManager.Application.Abstractions.Persistence.Common;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Persists budget categories and exposes category-usage checks.
/// </summary>
public interface IBudgetCategoryRepository : INamedEntityRepository<BudgetCategory>
{
    Task<bool> IsUsedAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsUsedAsync(Guid categoryId, Guid budgetId, CancellationToken cancellationToken);
}
