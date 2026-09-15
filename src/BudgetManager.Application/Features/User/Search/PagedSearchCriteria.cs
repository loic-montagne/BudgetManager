using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;

namespace BudgetManager.Application.Features.User.Search;

public sealed record PagedSearchCriteria(bool? IsActivated, string? Search, int? Offset, int? Limit, IReadOnlyList<SortCriterion<UserSortField>>? Sorts) : PagedSearchCriteria<UserSortField>(Search, Offset, Limit, Sorts);
