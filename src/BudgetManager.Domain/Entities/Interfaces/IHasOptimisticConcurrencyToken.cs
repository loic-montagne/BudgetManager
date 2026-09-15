namespace BudgetManager.Domain.Entities.Interfaces;

public interface IHasOptimisticConcurrencyToken
{
    /// <summary>
    /// Gets the optimistic-concurrency token maintained by the persistence layer.
    /// </summary>
    byte[] RowVersion { get; }
}
