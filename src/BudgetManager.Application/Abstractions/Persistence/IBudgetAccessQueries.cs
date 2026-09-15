using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Provides read-only budget-access projections while enforcing budget-level permissions.
/// </summary>
public interface IBudgetAccessQueries
{
    /// <summary>
    /// Gets one access entry after validating the current user's permission on the budget.
    /// </summary>
    Task<Features.BudgetAccess.GetByKey.BudgetAccessDto?> GetByKeyAsync(Guid budgetId, Guid userId, Guid currentUserId, Permission requiredBudgetPermission, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all access entries after validating the current user's permission on the budget.
    /// </summary>
    Task<IReadOnlyCollection<Features.Budget.GetAccesses.BudgetAccessDto>> GetByBudgetIdAsync(Guid budgetId, Guid currentUserId, Permission requiredBudgetPermission, CancellationToken cancellationToken);
}
