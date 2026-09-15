using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;

namespace BudgetManager.Application.Contexts;

internal sealed class AccountContext(IAccountRepository accountRepository, EntityCacheContext cache) : IAccountContext
{
    public async Task<Domain.Entities.Account?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (cache.TryGet<Domain.Entities.Account>(id, out var cachedAccount))
            return cachedAccount;

        var account = await accountRepository.GetTrackedByIdAsync(id, cancellationToken);

        if (account is not null)
            cache.Set(id, account);

        return account;
    }

    public async Task<Domain.Entities.Account> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await GetAsync(id, cancellationToken);
        NotFoundException<Domain.Entities.Account>.ThrowIfNull(account, id);
        return account!;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await GetAsync(id, cancellationToken);
        return account != null;
    }

    public async Task<bool> IsOpenedAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await GetAsync(id, cancellationToken);
        return !(account?.IsClosed ?? true);
    }

    public async Task<bool> IsClosedAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await GetAsync(id, cancellationToken);
        return account?.IsClosed ?? false;
    }
}
