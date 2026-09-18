using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;

namespace BudgetManager.Application.Contexts;

internal sealed class BudgetCategoryContext(IBudgetCategoryRepository categoryRepository, EntityCacheContext cache) : IBudgetCategoryContext
{
    public async Task<Domain.Entities.BudgetCategory?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (cache.TryGet<Domain.Entities.BudgetCategory>(id, out var cachedCategory))
            return cachedCategory;

        var category = await categoryRepository.GetTrackedByIdAsync(id, cancellationToken);

        if (category is not null)
            cache.Set(id, category);

        return category;
    }

    public async Task<Domain.Entities.BudgetCategory> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await GetAsync(id, cancellationToken);
        NotFoundException<Domain.Entities.BudgetCategory>.ThrowIfNull(category, id);
        return category!;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await GetAsync(id, cancellationToken);
        return category != null;
    }

    public async Task<bool> IsAssociatedToBudgetAsync(Guid categoryId, Guid budgetId, CancellationToken cancellationToken)
    {
        var category = await GetAsync(categoryId, cancellationToken);
        return category?.AssociatedBudgets?.Any(x => x.BudgetId == budgetId) ?? false;
    }
}
