using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.Enums;
using BudgetManager.Infrastructure.Extensions;
using BudgetManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Queries;

internal sealed class TransactionQueries(ApplicationDbContext context) : ITransactionQueries
{
    public async Task<Application.Features.Transaction.GetById.TransactionDto?> GetByIdAsync(Guid id, Guid currentUserId, Permission requiredBudgetPermission, CancellationToken cancellationToken)
    {
        var data = await context
            .Set<Transaction>()
            .AsNoTracking()
            .Include(x => x.Budget)
            .Include(x => x.Category)
            .Include(x => x.Account)
                .ThenInclude(x => x.Bank)
            .Include(x => x.TransferAccount)
                .ThenInclude(x => x!.Bank)
            .Where(x => x.Id == id)
            .Where(x => x.Budget.Accesses.Any(x => x.UserId == currentUserId
                                                && (x.IsOwner || (EF.Property<Permission>(x, BudgetAccess.PermissionsPropertyName) & requiredBudgetPermission) == requiredBudgetPermission)))
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.CreatedBy,
                    x => x.Id,
                    (t, u) => new
                    {
                        Transaction = t,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.Transaction,
                    CreatedByUser = user
                })
            .GroupJoin(
                context.Set<ApplicationUser>(),
                    x => x.Transaction.UpdatedBy,
                    x => x.Id,
                    (a, u) => new
                    {
                        a.Transaction,
                        a.CreatedByUser,
                        Users = u
                    })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.Transaction,
                    x.CreatedByUser,
                    UpdatedByUser = user
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
            return null;

        return new Application.Features.Transaction.GetById.TransactionDto(
            data.Transaction.Id,
            data.Transaction.Name,
            data.Transaction.Type,
            data.Transaction.Amount,
            data.Transaction.SignedAmount,
            data.Transaction.Method,
            new Application.Features.Transaction.GetById.BudgetDto(
                data.Transaction.Budget.Id,
                data.Transaction.Budget.Name),
            new Application.Features.Transaction.GetById.BudgetCategoryDto(
                data.Transaction.Category.Id,
                data.Transaction.Category.Name,
                data.Transaction.Category.Description),
            new Application.Features.Transaction.GetById.AccountDto(
                data.Transaction.Account.Id,
                data.Transaction.Account.Name,
                data.Transaction.Account.IsClosed,
                data.Transaction.Account.Iban.Value,
                data.Transaction.Account.Bank.Name,
                data.Transaction.Account.Bank.Bic.Value),
            data.Transaction.TransferAccountId == null || data.Transaction.TransferAccountId == Guid.Empty ?
                null :
                new Application.Features.Transaction.GetById.AccountDto(
                    data.Transaction.TransferAccount!.Id,
                    data.Transaction.TransferAccount.Name,
                    data.Transaction.TransferAccount.IsClosed,
                    data.Transaction.TransferAccount.Iban.Value,
                    data.Transaction.TransferAccount.Bank.Name,
                    data.Transaction.TransferAccount.Bank.Bic.Value),
            data.Transaction.CreatedBy,
            data.CreatedByUser.GetDisplayName(data.Transaction.CreatedBy),
            data.Transaction.CreatedOn,
            data.Transaction.UpdatedBy,
            data.UpdatedByUser.GetDisplayName(data.Transaction.UpdatedBy),
            data.Transaction.UpdatedOn);
    }
}
