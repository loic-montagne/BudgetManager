using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;

namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Provides read-only account projections.
/// </summary>
public interface IAccountQueries
{
    Task<IReadOnlyCollection<Features.Account.GetAll.AccountDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<Features.Account.GetById.AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<Features.Account.Search.AccountDto>> SearchAsync(Features.Account.Search.PagedSearchCriteria criteria, CancellationToken cancellationToken);
}
