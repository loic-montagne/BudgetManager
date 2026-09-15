using BudgetManager.Application.Abstractions.Persistence.Common;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.ValueObjects;

namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Persists accounts and exposes account-specific consistency checks.
/// </summary>
public interface IAccountRepository : INamedEntityRepository<Account>
{
    Task<bool> IsIbanUniqueAsync(Iban iban, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> IsUsedAsync(Guid id, CancellationToken cancellationToken);
}
