using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository(ApplicationDbContext context)
        : ITransactionRepository
{
    public async Task<Transaction?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.FindAsync<Transaction>(id, cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(string name, Guid budgetId, Guid categoryId, Guid? excludingId, CancellationToken cancellationToken)
    {
        return !await context
            .Set<Transaction>()
            .AnyAsync(x => x.Name == name
                        && x.BudgetId == budgetId
                        && x.CategoryId == categoryId
                        && (excludingId == null || x.Id != excludingId),
                      cancellationToken);
    }
}
