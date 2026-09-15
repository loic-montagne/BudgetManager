using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;

namespace BudgetManager.Application.Features.Account.Search;

public sealed record PagedSearchCriteria(bool? IsClosed, bool AllBanks, IReadOnlyList<Guid> Banks, string? Search, int? Offset, int? Limit, IReadOnlyList<SortCriterion<AccountSortField>>? Sorts) : PagedSearchCriteria<AccountSortField>(Search, Offset, Limit, Sorts);
