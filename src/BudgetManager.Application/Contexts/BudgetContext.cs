using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Enums;
using BudgetManager.Application.Exceptions;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Exceptions;

namespace BudgetManager.Application.Contexts;

/// <summary>
/// Loads the complete tracked budget aggregate once per scope and exposes predicates tailored to application validation.
/// </summary>
internal sealed class BudgetContext(IBudgetRepository budgetRepository, EntityCacheContext cache, ICurrentUser currentUser) : IBudgetContext
{
    public async Task<Domain.Entities.Budget?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (cache.TryGet<Domain.Entities.Budget>(id, out var cachedBudget))
            return cachedBudget;

        var budget = await budgetRepository.GetTrackedByIdAsync(id, cancellationToken);

        if (budget is not null)
            cache.Set(id, budget);

        return budget;
    }

    public async Task<Domain.Entities.Budget> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var budget = await GetAsync(id, cancellationToken);
        NotFoundException<Domain.Entities.Budget>.ThrowIfNull(budget, id);
        return budget!;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        var budget = await GetAsync(id, cancellationToken);
        return budget != null;
    }

    public async Task<bool> IsOwnerAsync(Guid budgetId, Guid userId, CancellationToken cancellationToken)
    {
        var budget = await GetAsync(budgetId, cancellationToken);
        if (budget is null)
            return false;

        try
        {
            budget.EnsureIsOwner(userId);
            return true;
        }
        catch (UserIsNotOwnerException)
        {
            return false;
        }
    }

    public async Task<bool> IsNotOwnerAsync(Guid budgetId, Guid userId, CancellationToken cancellationToken)
    {
        var budget = await GetAsync(budgetId, cancellationToken);
        if (budget is null)
            return true;

        try
        {
            budget.EnsureIsNotOwner(userId);
            return true;
        }
        catch (UserIsOwnerException)
        {
            return false;
        }
    }

    public async Task<bool> HasCurrentUserPermissionAsync(Guid id, Permission permission, CancellationToken cancellationToken)
    {
        var budget = await GetAsync(id, cancellationToken);
        if (budget is null)
            return false;

        try
        {
            budget.EnsurePermission(currentUser.RequiredUserId, permission);
            return true;
        }
        catch (UnauthorizedBudgetAccessException)
        {
            return false;
        }
    }

    public async Task<BudgetEditableStatus> IsEditableAsync(Guid id, CancellationToken cancellationToken)
    {
        var budget = await GetAsync(id, cancellationToken);
        if (budget is null)
            return BudgetEditableStatus.NotAuthorized;

        try
        {
            budget.EnsureEditable(currentUser.RequiredUserId);
            return BudgetEditableStatus.Editable;
        }
        catch (UnauthorizedBudgetAccessException)
        {
            return BudgetEditableStatus.NotAuthorized;
        }
        catch (BudgetLockedException)
        {
            return BudgetEditableStatus.Locked;
        }
    }

    public async Task<BudgetLockableStatus> IsLockableAsync(Guid id, CancellationToken cancellationToken)
    {
        var budget = await GetAsync(id, cancellationToken);
        if (budget is null)
            return BudgetLockableStatus.NotAuthorized;

        try
        {
            budget.EnsureLockable(currentUser.RequiredUserId);
            return BudgetLockableStatus.Lockable;
        }
        catch (UnauthorizedBudgetAccessException)
        {
            return BudgetLockableStatus.NotAuthorized;
        }
        catch (BudgetAlreadyLockedException)
        {
            return BudgetLockableStatus.AlreadyLocked;
        }
    }

    public async Task<BudgetUnlockableStatus> IsUnlockableAsync(Guid id, CancellationToken cancellationToken)
    {
        var budget = await GetAsync(id, cancellationToken);
        if (budget is null)
            return BudgetUnlockableStatus.NotAuthorized;

        try
        {
            budget.EnsureUnlockable(currentUser.RequiredUserId);
            return BudgetUnlockableStatus.Unlockable;
        }
        catch (UnauthorizedBudgetAccessException)
        {
            return BudgetUnlockableStatus.NotAuthorized;
        }
        catch (BudgetAlreadyUnlockedException)
        {
            return BudgetUnlockableStatus.AlreadyUnlocked;
        }
    }
}
