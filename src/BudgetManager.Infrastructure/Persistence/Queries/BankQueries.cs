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

internal sealed class BankQueries(ApplicationDbContext context, ILogger<BankQueries> logger) : IBankQueries
{
    public async Task<IReadOnlyCollection<Application.Features.Bank.GetAll.BankDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context
            .Set<Bank>()
            .AsNoTracking()
            .Select(x => new Application.Features.Bank.GetAll.BankDto(
                x.Id,
                x.Name,
                x.Bic.Value,
                x.Accounts.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<Application.Features.Bank.GetById.BankDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var data = await context
            .Set<Bank>()
            .AsNoTracking()
            .Include(x => x.Accounts)
            .Where(x => x.Id == id)
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.CreatedBy,
                    x => x.Id,
                    (b, u) => new
                    {
                        Bank = b,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.Bank,
                    CreatedByUser = user
                })
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.Bank.UpdatedBy,
                    x => x.Id,
                    (a, u) => new
                    {
                        a.Bank,
                        a.CreatedByUser,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.Bank,
                    x.CreatedByUser,
                    UpdatedByUser = user
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
            return null;

        return new Application.Features.Bank.GetById.BankDto(
            data.Bank.Id,
            data.Bank.Name,
            data.Bank.Bic.Value,
            data.Bank.Accounts.Count,
            data.Bank.CreatedBy,
            data.CreatedByUser.GetDisplayName(data.Bank.CreatedBy),
            data.Bank.CreatedOn,
            data.Bank.UpdatedBy,
            data.UpdatedByUser.GetDisplayName(data.Bank.UpdatedBy),
            data.Bank.UpdatedOn);
    }

    public async Task<PagedResult<Application.Features.Bank.Search.BankDto>> SearchAsync(PagedSearchCriteria<BankSortField> criteria, CancellationToken cancellationToken)
    {
        var searchCriteria = criteria.GetLikePatternSearchValues();
        Expression<Func<Bank, bool>> searchPredicate = _ => false;
        foreach (var search in searchCriteria)
        {
            var s = search;
            Expression<Func<Bank, bool>> currentPredicate =
                x =>
                    EF.Functions.Like(x.Name, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter)
                 || EF.Functions.Like(x.Bic.Value, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter);
            searchPredicate = searchPredicate.Or(currentPredicate);
        }

        return await context
            .Set<Bank>()
            .AsNoTracking()
            .GetPagedResultAsync(
                criteria,
                logger,
                x => new Application.Features.Bank.Search.BankDto(
                    x.Id,
                    x.Name,
                    x.Bic.Value,
                    x.Accounts.Count),
                null,
                searchPredicate,
                x => x.Id,
                sort =>
                {
                    Expression<Func<Bank, object?>> keySelector =
                        sort.Field switch
                        {
                            BankSortField.Bic => x => x.Bic.Value,
                            BankSortField.AccountsCount => x => x.Accounts.Count,
                            _ => x => x.Name,
                        };
                    return keySelector;
                },
                cancellationToken);
    }
}
