using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.ValueObjects;
using BudgetManager.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Repositories;

internal sealed class BankRepository(ApplicationDbContext context)
        : GenericRepository<Bank>(context), IBankRepository
{
    public async Task<Bank?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.FindAsync<Bank>(id, cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        return !await _context
            .Set<Bank>()
            .AnyAsync(x => x.Name == name
                        && (excludingId == null || x.Id != excludingId),
                      cancellationToken);
    }

    public async Task<bool> IsBicUniqueAsync(Bic bic, Guid? excludingId, CancellationToken cancellationToken)
    {
        return !await _context
            .Set<Bank>()
            .AnyAsync(x => x.Bic == bic
                        && (excludingId == null || x.Id != excludingId),
                      cancellationToken);
    }

    public async Task<bool> IsUsedAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context
            .Set<Account>()
            .AnyAsync(x => x.BankId == id, cancellationToken);
    }
}
