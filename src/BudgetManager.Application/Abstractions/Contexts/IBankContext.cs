namespace BudgetManager.Application.Abstractions.Contexts;

/// <summary>
/// Provides scoped access to banks and existence checks used by application use cases.
/// </summary>
public interface IBankContext : IEntityContext<Domain.Entities.Bank>
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
