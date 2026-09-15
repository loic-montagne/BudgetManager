using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Provides read-only transaction projections secured by permissions on their owning budget.
/// </summary>
public interface ITransactionQueries
{
    /// <summary>
    /// Gets a transaction only when the user has the required permission on its budget.
    /// </summary>
    Task<Features.Transaction.GetById.TransactionDto?> GetByIdAsync(Guid id, Guid currentUserId, Permission requiredBudgetPermission, CancellationToken cancellationToken);
}
