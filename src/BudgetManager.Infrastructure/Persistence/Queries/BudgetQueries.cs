using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Extensions;
using BudgetManager.Infrastructure.Extensions;
using BudgetManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BudgetManager.Infrastructure.Persistence.Queries;

internal sealed class BudgetQueries(ApplicationDbContext context, ILogger<BudgetQueries> logger) : IBudgetQueries
{
    public async Task<IReadOnlyCollection<Application.Features.Budget.GetAll.BudgetDto>> GetAllAsync(Guid currentUserId, Permission requiredPermission, CancellationToken cancellationToken)
    {
        return await context
            .Set<Budget>()
            .AsNoTracking()
            .Where(x => x.Accesses.Any(x => x.UserId == currentUserId
                                         && (x.IsOwner || (EF.Property<Permission>(x, BudgetAccess.PermissionsPropertyName) & requiredPermission) == requiredPermission)))
            .OrderBy(x => x.Name)
            .Select(x => new Application.Features.Budget.GetAll.BudgetDto(
                x.Id,
                x.Name,
                x.IsLocked))
            .ToListAsync(cancellationToken);
    }

    public async Task<Application.Features.Budget.GetById.BudgetDto?> GetByIdAsync(Guid id, Guid currentUserId, Permission requiredPermission, CancellationToken cancellationToken)
    {
        var budget = await context
            .Set<Budget>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Where(x => x.Accesses.Any(x => x.UserId == currentUserId 
                                         && (x.IsOwner || (EF.Property<Permission>(x, BudgetAccess.PermissionsPropertyName) & requiredPermission) == requiredPermission)))
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.CreatedBy,
                    x => x.Id,
                    (b, u) => new
                    {
                        Budget = new
                        {
                            b.Id,
                            b.Name,
                            b.IsLocked,

                            Expenses = b.Transactions
                                .Where(t => t.Type == TransactionType.Expense)
                                .Sum(t => -(decimal)t.Amount),
                            Incomes = b.Transactions
                                .Where(t => t.Type == TransactionType.Income)
                                .Sum(t => (decimal)t.Amount),

                            Balance = b.Transactions.Sum(t =>
                                t.Type == TransactionType.Income
                                    ? (decimal)t.Amount
                                    : -(decimal)t.Amount),

                            CurrentUserAccess = b.Accesses
                                .Where(x => x.UserId == currentUserId)
                                .Select(access => new
                                {
                                    access.IsOwner,
                                    Permissions = EF.Property<Permission>(
                                        access,
                                        BudgetAccess.PermissionsPropertyName)
                                })
                                .First(),

                            b.CreatedBy,
                            b.CreatedOn,
                            b.UpdatedBy,
                            b.UpdatedOn
                        },
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.Budget,
                    CreatedByUser = user
                })
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.Budget.UpdatedBy,
                    x => x.Id,
                    (a, u) => new
                    {
                        a.Budget,
                        a.CreatedByUser,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.Budget,
                    x.CreatedByUser,
                    UpdatedByUser = user
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (budget is null)
            return null;

        var accesses = await context
            .Set<BudgetAccess>()
            .AsNoTracking()
            .Where(x => x.BudgetId == id)
            .Join(
                context.Set<ApplicationUser>(),
                x => x.UserId,
                x => x.Id,
                (a, u) => new
                {
                    User = new Application.Features.User.Common.UserDto(
                        u.Id,
                        u.UserName ?? string.Empty,
                        u.LastName,
                        u.FirstName),
                    a.IsOwner,
                    Permissions = EF.Property<Permission>(a, BudgetAccess.PermissionsPropertyName)
                })
            .ToListAsync(cancellationToken);

        var categories = await context
            .Set<BudgetCategoryAssociation>()
            .AsNoTracking()
            .Where(x => x.BudgetId == id)
            .OrderBy(x => x.Order)
            .Select(x =>
                new Application.Features.Budget.GetById.BudgetCategoryDto(
                    x.Category.Id,
                    x.Category.Name,
                    x.Category.Description,
                    x.Order,
                    context
                        .Set<Transaction>()
                        .Where(t =>
                            t.BudgetId == id &&
                            t.CategoryId == x.CategoryId &&
                            t.Type == TransactionType.Expense)
                        .Sum(t => -(decimal)t.Amount),
                    context
                        .Set<Transaction>()
                        .Where(t =>
                            t.BudgetId == id &&
                            t.CategoryId == x.CategoryId &&
                            t.Type == TransactionType.Income)
                        .Sum(t => (decimal)t.Amount),
                    context
                        .Set<Transaction>()
                        .Where(t =>
                            t.BudgetId == id &&
                            t.CategoryId == x.CategoryId)
                        .Sum(t =>
                            t.Type == TransactionType.Income
                                ? (decimal)t.Amount
                                : -(decimal)t.Amount)))
            .ToListAsync(cancellationToken);

        var transactions = await context
            .Set<Transaction>()
            .AsNoTracking()
            .Where(x => x.BudgetId == id)
            .Select(x =>
                new Application.Features.Budget.GetById.TransactionDto(
                    x.Id,
                    x.Name,
                    new Application.Features.Budget.GetById.BudgetCategoryDto(
                        x.Category.Id,
                        x.Category.Name,
                        x.Category.Description,
                        context
                            .Set<BudgetCategoryAssociation>()
                            .Where(a => a.BudgetId == id && a.CategoryId == x.CategoryId)
                            .Select(a => a.Order)
                            .Single(),
                        context
                            .Set<Transaction>()
                            .Where(t => t.BudgetId == id && t.CategoryId == x.CategoryId && t.Type == TransactionType.Expense)
                            .Sum(t => -(decimal)t.Amount),
                        context
                            .Set<Transaction>()
                            .Where(t => t.BudgetId == id && t.CategoryId == x.CategoryId && t.Type == TransactionType.Income)
                            .Sum(t => (decimal)t.Amount),
                        context
                            .Set<Transaction>()
                            .Where(t => t.BudgetId == id && t.CategoryId == x.CategoryId)
                            .Sum(t =>
                                t.Type == TransactionType.Income
                                    ? (decimal)t.Amount
                                    : -(decimal)t.Amount)),
                        x.Type,
                        x.Amount,
                        x.Type == TransactionType.Income
                            ? (decimal)x.Amount
                            : -(decimal)x.Amount,
                        x.Method,
                        context
                            .Set<Account>()
                            .Where(a => a.Id == x.AccountId)
                            .Select(a => new Application.Features.Budget.GetById.AccountDto(
                                a.Id,
                                a.Name,
                                a.IsClosed,
                                a.Iban.Value,
                                new Application.Features.Budget.GetById.BankDto(
                                    a.Bank.Id,
                                    a.Bank.Name,
                                    a.Bank.Bic.Value)))
                            .Single(),
                        x.TransferAccountId == null ? null :
                        context
                            .Set<Account>()
                            .Where(a => a.Id == x.TransferAccountId)
                            .Select(a => new Application.Features.Budget.GetById.AccountDto(
                                a.Id,
                                a.Name,
                                a.IsClosed,
                                a.Iban.Value,
                                new Application.Features.Budget.GetById.BankDto(
                                    a.Bank.Id,
                                    a.Bank.Name,
                                    a.Bank.Bic.Value)))
                            .Single()))
            .ToListAsync(
                cancellationToken);

        return new Application.Features.Budget.GetById.BudgetDto(
            budget.Budget.Id,
            budget.Budget.Name,
            budget.Budget.IsLocked,
            budget.Budget.Expenses,
            budget.Budget.Incomes,
            budget.Budget.Balance,
            [.. accesses
                .Select(x =>
                    new Application.Features.Budget.GetById.AccessDto(
                        x.User,
                        x.IsOwner,
                        x.IsOwner
                            ? Permission.All.GetPermissions()
                            : x.Permissions.GetPermissions()))],
            categories,
            transactions,
            budget.Budget.CurrentUserAccess.IsOwner,
            budget.Budget.CurrentUserAccess.IsOwner
                ? Permission.All.GetPermissions()
                : budget.Budget.CurrentUserAccess.Permissions.GetPermissions(),
            budget.Budget.CreatedBy,
            budget.CreatedByUser.GetDisplayName(budget.Budget.CreatedBy),
            budget.Budget.CreatedOn,
            budget.Budget.UpdatedBy,
            budget.UpdatedByUser.GetDisplayName(budget.Budget.UpdatedBy),
            budget.Budget.UpdatedOn);
    }

    public async Task<PagedResult<Application.Features.Budget.Search.BudgetDto>> SearchAsync(Application.Features.Budget.Search.PagedSearchCriteria criteria, Guid currentUserId, Permission requiredPermission, CancellationToken cancellationToken)
    {
        var searchCriteria = criteria.GetLikePatternSearchValues();
        Expression<Func<Budget, bool>> searchPredicate = _ => false;
        foreach (var search in searchCriteria)
        {
            var s = search;
            Expression<Func<Budget, bool>> currentPredicate =
                x =>
                    EF.Functions.Like(x.Name, s, PagedSearchCriteriaExtensions.EscapeLikeCharacter);
            searchPredicate = searchPredicate.Or(currentPredicate);
        }


        return await context
            .Set<Budget>()
            .AsNoTracking()
            .Where(x => 
                x.Accesses.Any(x => x.UserId == currentUserId
                                && (x.IsOwner || (EF.Property<Permission>(x, BudgetAccess.PermissionsPropertyName) & requiredPermission) == requiredPermission)))
            .GetPagedResultAsync(
                criteria,
                logger,
                x => new Application.Features.Budget.Search.BudgetDto(
                    x.Id,
                    x.Name,
                    x.IsLocked),
                x => criteria.IsLocked == null ||
                     x.IsLocked == criteria.IsLocked,
                searchPredicate,
                x => x.Id,
                sort =>
                {
                    Expression<Func<Budget, object?>> keySelector =
                        sort.Field switch
                        {
                            BudgetSortField.IsLocked => x => x.IsLocked,
                            _ => x => x.Name,
                        };
                    return keySelector;
                },
                cancellationToken);
    }
}
