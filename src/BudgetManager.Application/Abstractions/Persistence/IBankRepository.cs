using BudgetManager.Application.Abstractions.Persistence.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.ValueObjects;

namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Persists banks and exposes bank-specific consistency checks.
/// </summary>
public interface IBankRepository : INamedEntityRepository<Bank>
{
    Task<bool> IsBicUniqueAsync(Bic bic, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> IsUsedAsync(Guid id, CancellationToken cancellationToken);
}
