using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Domain.Entities;
using BudgetManager.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Repositories;

internal sealed class BudgetCategoryRepository(ApplicationDbContext context)
        : GenericRepository<BudgetCategory>(context), IBudgetCategoryRepository
{
    public async Task<BudgetCategory?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context
            .Set<BudgetCategory>()
            .Include(x => x.Budgets)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        return !await _context
            .Set<BudgetCategory>()
            .AnyAsync(x => x.Name == name
                        && (excludingId == null || x.Id != excludingId),
                      cancellationToken);
    }

    public async Task<bool> IsUsedAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context
            .Set<Transaction>()
            .AnyAsync(x => x.CategoryId == id, cancellationToken);
    }

    public async Task<bool> IsUsedAsync(Guid categoryId, Guid budgetId, CancellationToken cancellationToken)
    {
        return await _context
            .Set<Transaction>()
            .AnyAsync(x => x.BudgetId == budgetId && x.CategoryId == categoryId, cancellationToken);
    }
}
