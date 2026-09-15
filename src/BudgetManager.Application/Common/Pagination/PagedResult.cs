namespace BudgetManager.Application.Common.Pagination;

public sealed record PagedResult<TEntity>(IReadOnlyCollection<TEntity> Results, int TotalCount, int FilteredCount, int ReturnedCount, int? Offset, int? Limit) where TEntity : class;
