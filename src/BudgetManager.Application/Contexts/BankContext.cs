using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;

namespace BudgetManager.Application.Contexts;

internal sealed class BankContext(IBankRepository bankRepository, EntityCacheContext cache) : IBankContext
{
    public async Task<Domain.Entities.Bank?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (cache.TryGet<Domain.Entities.Bank>(id, out var cachedBank))
            return cachedBank;

        var bank = await bankRepository.GetTrackedByIdAsync(id, cancellationToken);

        if (bank is not null)
            cache.Set(id, bank);

        return bank;
    }

    public async Task<Domain.Entities.Bank> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var bank = await GetAsync(id, cancellationToken);
        NotFoundException<Domain.Entities.Bank>.ThrowIfNull(bank, id);
        return bank!;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        var bank = await GetAsync(id, cancellationToken);
        return bank != null;
    }

}
