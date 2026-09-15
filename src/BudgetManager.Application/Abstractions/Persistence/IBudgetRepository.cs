using BudgetManager.Application.Abstractions.Persistence.Common;
using BudgetManager.Domain.Entities;

namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Persists <see cref="Budget"/> aggregates.
/// </summary>
/// <remarks>
/// The inherited tracked-loading operation must load the complete aggregate required by mutations, including accesses, categories and transactions.
/// </remarks>
public interface IBudgetRepository : INamedEntityRepository<Budget>
{
    Task TransferOwnershipAsync(Budget budget, Guid previousOwnerId, CancellationToken cancellationToken);
}
