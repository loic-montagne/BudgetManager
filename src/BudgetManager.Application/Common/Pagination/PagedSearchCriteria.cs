namespace BudgetManager.Application.Common.Pagination;

public record PagedSearchCriteria<TSortFieldEnum>(string? Search, int? Offset, int? Limit, IReadOnlyList<SortCriterion<TSortFieldEnum>>? Sorts) where TSortFieldEnum : struct, Enum;
