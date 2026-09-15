using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BudgetManager.Infrastructure.Extensions;

internal static class IQueryableExtensions
{
    public static async Task<PagedResult<TDto>> GetPagedResultAsync<TEntity, TDto, TSortFieldEnum, TQuery>(
        this IQueryable<TEntity> query,
        PagedSearchCriteria<TSortFieldEnum> criteria,
        ILogger<TQuery> logger,
        Expression<Func<TEntity, TDto>> selectPredicate,
        Expression<Func<TEntity, bool>>? wherePredicate,
        Expression<Func<TEntity, bool>> searchWherePredicate,
        Expression<Func<TEntity, object>> defaultOrderBySelector,
        Func<SortCriterion<TSortFieldEnum>, Expression<Func<TEntity, object?>>?> getOrderBySelector,
        CancellationToken cancellationToken)
         where TEntity : class
         where TDto : class
         where TSortFieldEnum : struct, Enum
        where TQuery : class
    {
        // Total des entités
        logger.LogDebug("Counting {EntityType} entities.", typeof(TEntity).Name);
        int totalCount = await query.CountAsync(cancellationToken);

        // Filtres
        if (wherePredicate is not null)
            query = query.Where(wherePredicate);
        if (!string.IsNullOrWhiteSpace(criteria.Search))
            query = query.Where(searchWherePredicate);

        // Total des entités filtrées
        logger.LogDebug("Counting filtered {EntityType} entities.", typeof(TEntity).Name);
        var filteredCount = await query.CountAsync(cancellationToken);

        // Tris reçus en paramètres
        IOrderedQueryable<TEntity>? orderedQuery = null;
        if (criteria.Sorts != null && criteria.Sorts.Any())
        {
            foreach (var sort in criteria.Sorts)
            {
                var selector = getOrderBySelector.Invoke(sort);
                if (selector is null)
                    continue;

                orderedQuery = sort.Direction switch
                {
                    SortDirection.Descending when orderedQuery is null =>
                        query.OrderByDescending(selector),

                    SortDirection.Descending =>
                        orderedQuery.ThenByDescending(selector),

                    _ when orderedQuery is null =>
                        query.OrderBy(selector),

                    _ =>
                        orderedQuery.ThenBy(selector)
                };
            }
        }
        // Tri par défaut
        if (orderedQuery is null)
            orderedQuery = query.OrderBy(defaultOrderBySelector);
        else
            orderedQuery = orderedQuery.ThenBy(defaultOrderBySelector);
        query = orderedQuery;

        // Pagination
        if (criteria.Offset is not null && criteria.Limit is not null)
        {
            query = query.Skip(criteria.Offset.Value);
            if (criteria.Limit > 0)
                query = query.Take(criteria.Limit.Value);
        }

        // Exécution de la recherche
        logger.LogDebug("Executing paged {EntityType} query.", typeof(TEntity).Name);
        var data = await query
            .Select(selectPredicate)
            .ToListAsync(cancellationToken);

        // Renvoi des informations
        return new PagedResult<TDto>(
            data,
            totalCount,
            filteredCount,
            data.Count,
            criteria.Offset,
            criteria.Limit);
    }
}
