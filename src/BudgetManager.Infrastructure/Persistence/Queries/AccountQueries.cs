using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Infrastructure.Extensions;
using BudgetManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BudgetManager.Infrastructure.Persistence.Queries;

internal sealed class AccountQueries(ApplicationDbContext context, ILogger<AccountQueries> logger) : IAccountQueries
{
    public async Task<IReadOnlyCollection<Application.Features.Account.GetAll.AccountDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context
            .Set<Account>()
            .AsNoTracking()
            .Select(x => new Application.Features.Account.GetAll.AccountDto(
                x.Id,
                x.Name,
                x.IsClosed,
                x.Iban.Value,
                x.Bank.Name,
                x.Bank.Bic.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<Application.Features.Account.GetById.AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var data = await context
            .Set<Account>()
            .AsNoTracking()
            .Include(x => x.Bank)
            .Where(x => x.Id == id)
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.CreatedBy,
                    x => x.Id,
                    (a, u) => new
                    {
                        Account = a,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.Account,
                    CreatedByUser = user
                })
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.Account.UpdatedBy,
                    x => x.Id,
                    (a, u) => new
                    {
                        a.Account,
                        a.CreatedByUser,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.Account,
                    x.CreatedByUser,
                    UpdatedByUser = user
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
            return null;

        return new Application.Features.Account.GetById.AccountDto(
            data.Account.Id,
            data.Account.Name,
            data.Account.IsClosed,
            data.Account.Iban.Value,
            new Application.Features.Account.GetById.BankDto(
                data.Account.Bank.Id,
                data.Account.Bank.Name,
                data.Account.Bank.Bic.Value),
            data.Account.CreatedBy,
            data.CreatedByUser.GetDisplayName(data.Account.CreatedBy),
            data.Account.CreatedOn,
            data.Account.UpdatedBy,
            data.UpdatedByUser.GetDisplayName(data.Account.UpdatedBy),
            data.Account.UpdatedOn);
    }

    public async Task<PagedResult<Application.Features.Account.Search.AccountDto>> SearchAsync(Application.Features.Account.Search.PagedSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var searchCriteria = criteria.GetLikePatternSearchValues();
        Expression<Func<Account, bool>> searchPredicate = _ => false;
        foreach (var search in searchCriteria)
        {
            var s = search;
            Expression<Func<Account, bool>> currentPredicate =
                x =>
                    EF.Functions.Like(x.Name, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter)
                 || EF.Functions.Like(x.Iban.Value, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter)
                 || EF.Functions.Like(x.Bank.Name, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter)
                 || EF.Functions.Like(x.Bank.Bic.Value, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter);
            searchPredicate = searchPredicate.Or(currentPredicate);
        }

        return await context
            .Set<Account>()
            .AsNoTracking()
            .GetPagedResultAsync(
                criteria,
                logger,
                x => new Application.Features.Account.Search.AccountDto(
                    x.Id,
                    x.Name,
                    x.IsClosed,
                    x.Iban.Value,
                    x.Bank.Name,
                    x.Bank.Bic.Value),
                x => (criteria.IsClosed == null ||
                      criteria.IsClosed == x.IsClosed) &&
                     (criteria.AllBanks ||
                      criteria.Banks.Contains(x.BankId)),
                searchPredicate,
                x => x.Id,
                sort =>
                {
                    Expression<Func<Account, object?>> keySelector =
                        sort.Field switch
                        {
                            AccountSortField.IsClosed => x => x.IsClosed,
                            AccountSortField.BankName => x => x.Bank.Name,
                            AccountSortField.Iban => x => x.Iban.Value,
                            AccountSortField.Bic => x => x.Bank.Bic.Value,
                            _ => x => x.Name,
                        };
                    return keySelector;
                },
                cancellationToken);
    }
}
