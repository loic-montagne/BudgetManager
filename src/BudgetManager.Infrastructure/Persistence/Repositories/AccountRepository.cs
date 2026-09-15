using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Entities.Common;
using BudgetManager.Domain.ValueObjects;
using BudgetManager.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Repositories;

internal sealed class AccountRepository(ApplicationDbContext context)
        : GenericRepository<Account>(context), IAccountRepository
{
    public async Task<Account?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.FindAsync<Account>(id, cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        return !await _context
            .Set<Account>()
            .AnyAsync(x => x.Name == name
                        && (excludingId == null || x.Id != excludingId),
                      cancellationToken);
    }

    public async Task<bool> IsIbanUniqueAsync(Iban iban, Guid? excludingId, CancellationToken cancellationToken)
    {
        return !await _context
            .Set<Account>()
            .AnyAsync(x => x.Iban == iban
                        && (excludingId == null || x.Id != excludingId),
                      cancellationToken);
    }

    public async Task<bool> IsUsedAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context
            .Set<Transaction>()
            .AnyAsync(x => x.AccountId == id || x.TransferAccountId == id, cancellationToken);
    }
}
