using BudgetManager.Application.Enums;
using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Abstractions.Contexts;

/// <summary>
/// Provides scoped access to the complete tracked budget aggregate and exposes validation-oriented budget predicates.
/// </summary>
public interface IBudgetContext : IEntityContext<Domain.Entities.Budget>
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsOwnerAsync(Guid budgetId, Guid userId, CancellationToken cancellationToken);
    Task<bool> IsNotOwnerAsync(Guid budgetId, Guid userId, CancellationToken cancellationToken);
    Task<bool> HasCurrentUserPermissionAsync(Guid id, Permission permission, CancellationToken cancellationToken);
    Task<BudgetEditableStatus> IsEditableAsync(Guid id, CancellationToken cancellationToken);
    Task<BudgetLockableStatus> IsLockableAsync(Guid id, CancellationToken cancellationToken);
    Task<BudgetUnlockableStatus> IsUnlockableAsync(Guid id, CancellationToken cancellationToken);
}
