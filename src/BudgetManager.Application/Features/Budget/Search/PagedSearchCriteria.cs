using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;

namespace BudgetManager.Application.Features.Budget.Search;

public sealed record PagedSearchCriteria(bool? IsLocked, string? Search, int? Offset, int? Limit, IReadOnlyList<SortCriterion<BudgetSortField>>? Sorts) : PagedSearchCriteria<BudgetSortField>(Search, Offset, Limit, Sorts);
