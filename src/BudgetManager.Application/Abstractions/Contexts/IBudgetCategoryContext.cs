namespace BudgetManager.Application.Abstractions.Contexts;

/// <summary>
/// Provides scoped access to budget categories and their budget associations.
/// </summary>
public interface IBudgetCategoryContext : IEntityContext<Domain.Entities.BudgetCategory>
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsAssociatedToBudgetAsync(Guid categoryId, Guid budgetId, CancellationToken cancellationToken);
}
