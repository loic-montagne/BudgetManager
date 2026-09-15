namespace BudgetManager.Application.Abstractions.Contexts;

/// <summary>
/// Provides scoped access to accounts and predicates used by application validators.
/// </summary>
public interface IAccountContext : IEntityContext<Domain.Entities.Account>
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsOpenedAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsClosedAsync(Guid id, CancellationToken cancellationToken);
}
