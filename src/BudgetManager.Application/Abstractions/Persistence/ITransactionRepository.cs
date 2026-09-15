namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Provides command-side access and uniqueness checks for transactions.
/// </summary>
public interface ITransactionRepository
{
    Task<Domain.Entities.Transaction?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsNameUniqueAsync(string name, Guid budgetId, Guid categoryId, Guid? excludingId, CancellationToken cancellationToken);
}
