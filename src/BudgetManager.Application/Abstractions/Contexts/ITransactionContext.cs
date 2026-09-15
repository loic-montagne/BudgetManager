namespace BudgetManager.Application.Abstractions.Contexts;

/// <summary>
/// Provides scoped access to transactions and predicates describing their aggregate relationships.
/// </summary>
public interface ITransactionContext : IEntityContext<Domain.Entities.Transaction>
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsInBudgetAsync(Guid transactionId, Guid budgetId, CancellationToken cancellationToken);
    Task<bool> IsInCategoryAsync(Guid transactionId, Guid categoryId, CancellationToken cancellationToken);
    Task<bool> IsInBudgetAndCategoryAsync(Guid transactionId, Guid budgetId, Guid categoryId, CancellationToken cancellationToken);
}
