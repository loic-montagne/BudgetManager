using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Domain.Entities;
using BudgetManager.Infrastructure.Extensions;
using BudgetManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BudgetManager.Infrastructure.Persistence.Queries;

internal sealed class BudgetCategoryQueries(ApplicationDbContext context, ILogger<BudgetCategoryQueries> logger) : IBudgetCategoryQueries
{
    public async Task<IReadOnlyCollection<Application.Features.BudgetCategory.GetAll.BudgetCategoryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context
            .Set<BudgetCategory>()
            .AsNoTracking()
            .Select(x => new Application.Features.BudgetCategory.GetAll.BudgetCategoryDto(
                x.Id,
                x.Name,
                x.Description,
                x.AssociatedBudgets.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<Application.Features.BudgetCategory.GetById.BudgetCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var data = await context
            .Set<BudgetCategory>()
            .AsNoTracking()
            .Include(x => x.AssociatedBudgets)
            .Where(x => x.Id == id)
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.CreatedBy,
                    x => x.Id,
                    (b, u) => new
                    {
                        BudgetCategory = b,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.BudgetCategory,
                    CreatedByUser = user
                })
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.BudgetCategory.UpdatedBy,
                    x => x.Id,
                    (a, u) => new
                    {
                        a.BudgetCategory,
                        a.CreatedByUser,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.BudgetCategory,
                    x.CreatedByUser,
                    UpdatedByUser = user
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
            return null;

        return new Application.Features.BudgetCategory.GetById.BudgetCategoryDto(
            data.BudgetCategory.Id,
            data.BudgetCategory.Name,
            data.BudgetCategory.Description,
            data.BudgetCategory.AssociatedBudgets.Count,
            data.BudgetCategory.CreatedBy,
            data.CreatedByUser.GetDisplayName(data.BudgetCategory.CreatedBy),
            data.BudgetCategory.CreatedOn,
            data.BudgetCategory.UpdatedBy,
            data.UpdatedByUser.GetDisplayName(data.BudgetCategory.UpdatedBy),
            data.BudgetCategory.UpdatedOn);
    }

    public async Task<PagedResult<Application.Features.BudgetCategory.Search.BudgetCategoryDto>> SearchAsync(PagedSearchCriteria<BudgetCategorySortField> criteria, CancellationToken cancellationToken)
    {
        var searchCriteria = criteria.GetLikePatternSearchValues();
        Expression<Func<BudgetCategory, bool>> searchPredicate = _ => false;
        foreach (var search in searchCriteria)
        {
            var s = search;
            Expression<Func<BudgetCategory, bool>> currentPredicate =
                x =>
                    EF.Functions.Like(x.Name, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter)
                 || EF.Functions.Like(x.Description, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter);
            searchPredicate = searchPredicate.Or(currentPredicate);
        }

        return await context
            .Set<BudgetCategory>()
            .AsNoTracking()
            .GetPagedResultAsync(
                criteria,
                logger,
                x => new Application.Features.BudgetCategory.Search.BudgetCategoryDto(
                    x.Id,
                    x.Name,
                    x.Description,
                    x.AssociatedBudgets.Count),
                null,
                searchPredicate,
                x => x.Id,
                sort =>
                {
                    Expression<Func<BudgetCategory, object?>> keySelector =
                        sort.Field switch
                        {
                            BudgetCategorySortField.Description => x => x.Description,
                            BudgetCategorySortField.BudgetsCount => x => x.AssociatedBudgets.Count,
                            _ => x => x.Name,
                        };
                    return keySelector;
                },
                cancellationToken);
    }
}
