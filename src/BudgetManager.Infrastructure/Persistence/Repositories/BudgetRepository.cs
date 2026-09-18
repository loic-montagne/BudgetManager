using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Domain.Entities;
using BudgetManager.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BudgetManager.Infrastructure.Persistence.Repositories;

internal sealed class BudgetRepository(ApplicationDbContext context)
        : GenericRepository<Budget>(context), IBudgetRepository
{
    public override async Task UpdateAsync(Budget budget, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(budget);

        var entry = _context.Entry(budget);
        if (entry.State == EntityState.Detached)
            throw new InvalidOperationException("The budget must be tracked before it can be updated.");

        if (entry.State == EntityState.Unchanged)
            entry.State = EntityState.Modified;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task TransferOwnershipAsync(Budget budget, Guid previousOwnerId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(budget);

        var entry = _context.Entry(budget);
        if (entry.State == EntityState.Detached)
            throw new InvalidOperationException("The budget must be tracked before it can be updated.");


        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var affectedRows = await _context
                .Set<BudgetAccess>()
                .Where(x =>
                    x.BudgetId == budget.Id &&
                    x.UserId == previousOwnerId &&
                    x.IsOwner)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            x => x.IsOwner,
                            false),
                    cancellationToken);

            if (affectedRows != 1)
                throw new InvalidOperationException("The previous budget owner could not be updated.");

            if (entry.State == EntityState.Unchanged)
                entry.State = EntityState.Modified;

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Budget?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context
            .Set<Budget>()
            .Include(x => x.Accesses)
            .Include(x => x.AssociatedCategories)
            .ThenInclude(x => x.Category)
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        return !await _context
            .Set<Budget>()
            .AnyAsync(x => x.Name == name
                        && (excludingId == null || x.Id != excludingId),
                      cancellationToken);
    }
}
