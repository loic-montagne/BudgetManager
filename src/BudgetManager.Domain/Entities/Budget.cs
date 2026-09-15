using BudgetManager.Domain.Common;
using BudgetManager.Domain.Entities.Common;
using BudgetManager.Domain.Entities.Interfaces;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.Extensions;
using BudgetManager.Domain.ValueObjects;

namespace BudgetManager.Domain.Entities;

public sealed class Budget : Entity, IHasOptimisticConcurrencyToken
{
    public string Name { get; private set; } = string.Empty;
    public bool IsLocked { get; private set; }
    public decimal Balance => Transactions?.Sum(x => x.SignedAmount) ?? 0;


    private readonly List<BudgetAccess> _accesses = [];
    public IReadOnlyCollection<BudgetAccess> Accesses => _accesses.AsReadOnly();

    private readonly List<BudgetCategory> _categories = [];
    public IReadOnlyCollection<BudgetCategory> Categories => _categories.AsReadOnly();

    private readonly List<Transaction> _transactions = [];
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    /// <summary>
    /// Gets the optimistic-concurrency token maintained by the persistence layer.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private Budget() 
    { 
    }

    public static Budget Create(string name, Guid currentUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > StringPropertyLengths.NameLength)
            throw new ArgumentException($"Name cannot be longer than {StringPropertyLengths.NameLength} characters.", nameof(name));

        if (currentUserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(currentUserId));

        var budget = new Budget()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim()
        };
        budget._accesses.Add(BudgetAccess.Create(budget.Id, currentUserId, true, Permission.None));

        return budget;
    }

    
    public void Rename(string name, Guid currentUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > StringPropertyLengths.NameLength)
            throw new ArgumentException($"Name cannot be longer than {StringPropertyLengths.NameLength} characters.", nameof(name));
        EnsureEditable(currentUserId);
        Name = name.Trim();
    }
    public void Lock(Guid currentUserId)
    {
        EnsureLockable(currentUserId);
        IsLocked = true;
    }
    public void Unlock(Guid currentUserId)
    {
        EnsureUnlockable(currentUserId);
        IsLocked = false;
    }


    private BudgetAccess? GetAccess(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        return _accesses.FirstOrDefault(x => x.UserId == userId);
    }
    internal void EnsureIsOwner(Guid userId)
    {
        var access = GetAccess(userId);
        if (access == null || !access.IsOwner)
            throw new UserIsNotOwnerException();
    }
    internal void EnsureIsNotOwner(Guid userId)
    {
        var access = GetAccess(userId);
        if (access != null && access.IsOwner)
            throw new UserIsOwnerException();
    }
    internal void EnsurePermission(Guid userId, Permission permission)
    {
        permission.ThrowIfNotSingle(nameof(permission));
        var access = GetAccess(userId);
        if (access == null || !access.HasPermission(permission))
            throw new UnauthorizedBudgetAccessException();
    }
    internal void EnsureEditable(Guid userId)
    {
        EnsurePermission(userId, Permission.Edit);

        if (IsLocked)
            throw new BudgetLockedException();
    }
    internal void EnsureLockable(Guid userId)
    {
        EnsurePermission(userId, Permission.Lock);

        if (IsLocked)
            throw new BudgetAlreadyLockedException();
    }
    internal void EnsureUnlockable(Guid userId)
    {
        EnsurePermission(userId, Permission.Lock);

        if (!IsLocked)
            throw new BudgetAlreadyUnlockedException();
    }

    public IEnumerable<Permission> GetPermissions(Guid userId, Guid currentUserId)
    {
        EnsurePermission(currentUserId, Permission.Share);
        return GetAccess(userId)?.GetPermissions() ?? [];
    }
    public bool HasNoPermissions(Guid userId, Guid currentUserId)
    {
        EnsurePermission(currentUserId, Permission.Share);
        return GetAccess(userId)?.HasNoPermissions() ?? true;
    }
    public bool HasPermission(Guid userId, Permission permission, Guid currentUserId)
    {
        EnsurePermission(currentUserId, Permission.Share);
        return GetAccess(userId)?.HasPermission(permission) ?? false;
    }
    public bool HasAnyPermission(Guid userId, Permission permissions, Guid currentUserId)
    {
        EnsurePermission(currentUserId, Permission.Share);
        return GetAccess(userId)?.HasAnyPermission(permissions) ?? false;
    }
    public bool HasAllPermissions(Guid userId, Permission permissions, Guid currentUserId)
    {
        EnsurePermission(currentUserId, Permission.Share);
        return GetAccess(userId)?.HasAllPermissions(permissions) ?? false;
    }
    /// <summary>
    /// Replaces the permissions granted to a user.
    /// </summary>
    /// <remarks>
    /// Passing <see cref="Permission.None"/> removes an existing non-owner access.
    /// </remarks>
    public void SetPermissions(Guid userId, Permission permissions, Guid currentUserId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(currentUserId));

        EnsurePermission(currentUserId, Permission.Share);

        permissions.ThrowIfNotValid(nameof(permissions));

        var access = GetAccess(userId);
        if (access == null && permissions != Permission.None)
        {
            access = BudgetAccess.Create(Id, userId, false, permissions);
            _accesses.Add(access);
        }
        else if (access != null)
        {
            access.SetPermissions(permissions);
            if (access.HasNoPermissions())
            {
                _accesses.Remove(access);
            }
        }
    }
    public void GrantAccess(Guid userId, Permission permission, Guid currentUserId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(currentUserId));

        EnsurePermission(currentUserId, Permission.Share);

        permission.ThrowIfNone();
        permission.ThrowIfNotValid();
        permission.ThrowIfNotSingle();

        var access = GetAccess(userId);
        if (access == null)
        {
            access = BudgetAccess.Create(Id, userId, false, permission);
            _accesses.Add(access);
        }
        else
        {
            access.Grant(permission);
        }
    }
    public void RevokeAccess(Guid userId, Permission permission, Guid currentUserId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(currentUserId));

        EnsurePermission(currentUserId, Permission.Share);

        permission.ThrowIfNone();
        permission.ThrowIfNotValid();
        permission.ThrowIfNotSingle();

        var access = GetAccess(userId);
        if (access == null)
        {
            throw new PermissionNotGrantedException();
        }
        else
        {
            access.Revoke(permission);
            if (access.HasNoPermissions())
            {
                _accesses.Remove(access);
            }
        }
    }
    /// <summary>
    /// Transfers ownership of the budget to another user.
    /// </summary>
    /// <remarks>
    /// The previous owner remains a member of the budget with all permissions.
    /// </remarks>
    public void TransferOwnership(Guid userId, Guid currentUserId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(currentUserId));
        if (userId == currentUserId)
            throw new CannotTransferOwnershipToYourselfException();

        var oldOwnerAccess = GetAccess(currentUserId);
        if (oldOwnerAccess == null || !oldOwnerAccess.IsOwner)
            throw new UnauthorizedBudgetAccessException();

        var newOwnerAccess = GetAccess(userId);
        if (newOwnerAccess != null && newOwnerAccess.IsOwner)
            throw new UserAlreadyOwnerException();

        if (newOwnerAccess == null)
            newOwnerAccess = BudgetAccess.Create(Id, userId, true, Permission.None);
        else if (!newOwnerAccess.IsOwner)
            newOwnerAccess.PromoteToOwner();
        oldOwnerAccess.DemoteFromOwner();
        if (!_accesses.Contains(newOwnerAccess))
            _accesses.Add(newOwnerAccess);
    }

    public void AssociateCategory(BudgetCategory category, Guid currentUserId)
    {
        ArgumentNullException.ThrowIfNull(category);
        if (category.Id == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(category));

        EnsureEditable(currentUserId);

        if (_categories.Any(x => x.Id == category.Id))
            throw new BudgetCategoryAlreadyAssociatedException();

        _categories.Add(category);
    }
    public void DissociateCategory(Guid categoryId, Guid currentUserId)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(currentUserId));

        EnsureEditable(currentUserId);

        var category = _categories.SingleOrDefault(x => x.Id == categoryId)
            ?? throw new BudgetCategoryNotAssociatedException();
        if (_transactions.Any(x => x.CategoryId == categoryId))
            throw new BudgetCategoryInUseException();

        _categories.Remove(category);
    }

    private Transaction GetRequiredTransaction(Guid transactionId)
    {
        if (transactionId == Guid.Empty)
            throw new ArgumentException("TransactionId cannot be empty.", nameof(transactionId));

        var transaction = _transactions.SingleOrDefault(x => x.Id == transactionId);
        return transaction ?? throw new BudgetTransactionNotAssociatedException();
    }
    public Transaction AddTransaction(Guid categoryId, Guid accountId, string name, TransactionType type, UDecimal amount, PaymentMethod method, Guid? transferAccountId, Guid currentUserId)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(currentUserId));

        EnsureEditable(currentUserId);

        if (!_categories.Any(x => x.Id == categoryId))
            throw new BudgetCategoryNotAssociatedException();

        var transaction = Transaction.Create(Id, categoryId, accountId, name, type, amount, method, transferAccountId);
        _transactions.Add(transaction);

        return transaction;
    }
    public void RemoveTransaction(Guid transactionId, Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(currentUserId));

        EnsureEditable(currentUserId);

        var transaction = GetRequiredTransaction(transactionId);
        _transactions.Remove(transaction);
    }
    public void RenameTransaction(Guid transactionId, string name, Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(currentUserId));

        EnsureEditable(currentUserId);

        var transaction = GetRequiredTransaction(transactionId);
        transaction.Rename(name);
    }
    public void ChangeTransactionAmount(Guid transactionId, UDecimal amount, Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(currentUserId));

        EnsureEditable(currentUserId);

        var transaction = GetRequiredTransaction(transactionId);
        transaction.ChangeAmount(amount);
    }
    public void ChangeTransactionMethod(Guid transactionId, PaymentMethod method, Guid? transferAccountId, Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(currentUserId));

        EnsureEditable(currentUserId);

        var transaction = GetRequiredTransaction(transactionId);
        transaction.ChangeMethod(method, transferAccountId);
    }
}
