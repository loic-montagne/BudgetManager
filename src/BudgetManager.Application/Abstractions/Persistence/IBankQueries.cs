using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;

namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Provides read-only bank projections.
/// </summary>
public interface IBankQueries
{
    Task<IReadOnlyCollection<Features.Bank.GetAll.BankDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<Features.Bank.GetById.BankDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<Features.Bank.Search.BankDto>> SearchAsync(PagedSearchCriteria<BankSortField> criteria, CancellationToken cancellationToken);
}
