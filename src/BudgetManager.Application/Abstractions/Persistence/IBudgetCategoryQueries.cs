using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;

namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Provides read-only budget-category projections.
/// </summary>
public interface IBudgetCategoryQueries
{
    Task<IReadOnlyCollection<Features.BudgetCategory.GetAll.BudgetCategoryDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<Features.BudgetCategory.GetById.BudgetCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<Features.BudgetCategory.Search.BudgetCategoryDto>> SearchAsync(PagedSearchCriteria<BudgetCategorySortField> criteria, CancellationToken cancellationToken);
}
