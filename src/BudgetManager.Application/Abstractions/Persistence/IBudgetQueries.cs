using BudgetManager.Application.Common.Pagination;
using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Provides read-only projections of budgets.
/// </summary>
/// <remarks>
/// Implementations must apply the requested budget permission in the data query and return only budgets visible to the specified user.
/// </remarks>
public interface IBudgetQueries
{
    /// <summary>
    /// Gets all budgets for which the user has the required permission.
    /// </summary>
    Task<IReadOnlyCollection<Features.Budget.GetAll.BudgetDto>> GetAllAsync(Guid currentUserId, Permission requiredPermission, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a budget only when the user has the required permission.
    /// </summary>
    Task<Features.Budget.GetById.BudgetDto?> GetByIdAsync(Guid id, Guid currentUserId, Permission requiredPermission, CancellationToken cancellationToken);

    /// <summary>
    /// Searches budgets for which the user has the required permission.
    /// </summary>
    Task<PagedResult<Features.Budget.Search.BudgetDto>> SearchAsync(Features.Budget.Search.PagedSearchCriteria criteria, Guid currentUserId, Permission requiredPermission, CancellationToken cancellationToken);
}
