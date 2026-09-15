using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;

namespace BudgetManager.Application.Contexts;

internal sealed class TransactionContext(ITransactionRepository transactionRepository, EntityCacheContext cache) : ITransactionContext
{
    public async Task<Domain.Entities.Transaction?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (cache.TryGet<Domain.Entities.Transaction>(id, out var cachedTransaction))
            return cachedTransaction;

        var transaction = await transactionRepository.GetTrackedByIdAsync(id, cancellationToken);

        if (transaction is not null)
            cache.Set(id, transaction);

        return transaction;
    }

    public async Task<Domain.Entities.Transaction> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await GetAsync(id, cancellationToken);
        NotFoundException<Domain.Entities.Transaction>.ThrowIfNull(transaction, id);
        return transaction!;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await GetAsync(id, cancellationToken);
        return transaction != null;
    }

    public async Task<bool> IsInBudgetAsync(Guid transactionId, Guid budgetId, CancellationToken cancellationToken)
    {
        var transaction = await GetAsync(transactionId, cancellationToken);
        return transaction?.BudgetId == budgetId;
    }

    public async Task<bool> IsInCategoryAsync(Guid transactionId, Guid categoryId, CancellationToken cancellationToken)
    {
        var transaction = await GetAsync(transactionId, cancellationToken);
        return transaction?.CategoryId == categoryId;
    }

    public async Task<bool> IsInBudgetAndCategoryAsync(Guid transactionId, Guid budgetId, Guid categoryId, CancellationToken cancellationToken)
    {
        var transaction = await GetAsync(transactionId, cancellationToken);
        return transaction?.BudgetId == budgetId && transaction?.CategoryId == categoryId;
    }

}
