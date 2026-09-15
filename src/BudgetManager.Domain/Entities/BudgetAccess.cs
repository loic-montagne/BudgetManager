using BudgetManager.Domain.Entities.Common;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.Extensions;

namespace BudgetManager.Domain.Entities;

public sealed class BudgetAccess : Auditable
{
    public const string PermissionsPropertyName = nameof(Permissions);

    public Guid UserId { get; private set; }
    public Guid BudgetId { get; private set; }

    public Budget Budget { get; private set; } = null!;

    public bool IsOwner { get; private set; }
    private Permission Permissions { get; set; }

    private BudgetAccess()
    {
    }

    internal static BudgetAccess Create(Guid budgetId, Guid userId, bool isOwner, Permission permissions)
    {
        if (budgetId == Guid.Empty)
            throw new ArgumentException("BudgetId cannot be empty.", nameof(budgetId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (isOwner)
        {
            permissions = Permission.None;
        }
        else
        {
            permissions.ThrowIfNotValid(nameof(permissions));
            permissions.ThrowIfNone(nameof(permissions));
        }

        return new BudgetAccess()
        {
            UserId = userId,
            BudgetId = budgetId,
            IsOwner = isOwner,
            Permissions = permissions
        };
    }

    public IReadOnlyList<Permission> GetPermissions()
    {
        return (IsOwner ? Permission.All : Permissions).GetPermissions();
    }

    public bool HasNoPermissions()
    {
        return !IsOwner && Permissions.IsNone();
    }
    public bool HasPermission(Permission permission)
    {
        permission.ThrowIfNotSingle(nameof(permission));
        return IsOwner || Permissions.Includes(permission);
    }
    public bool HasAnyPermission(Permission permissions)
    {
        permissions.ThrowIfNotValid(nameof(permissions));
        permissions.ThrowIfNone(nameof(permissions));
        return IsOwner || Permissions.IncludesAny(permissions);
    }
    public bool HasAllPermissions(Permission permissions)
    {
        permissions.ThrowIfNotValid(nameof(permissions));
        permissions.ThrowIfNone(nameof(permissions));
        return IsOwner || Permissions.IncludesAll(permissions);
    }

    internal void PromoteToOwner()
    {
        if (IsOwner)
            throw new UserAlreadyOwnerException();
        IsOwner = true;
        Permissions = Permission.None;
    }
    internal void DemoteFromOwner()
    {
        if (!IsOwner)
            throw new UserNotAlreadyOwnerException();
        IsOwner = false;
        Permissions = Permission.All;
    }
    internal void SetPermissions(Permission permissions)
    {
        permissions.ThrowIfNotValid(nameof(permissions));
        if (IsOwner)
            throw new CannotGrantOwnerPermissionsException();
        Permissions = permissions;
    }
    internal void Grant(Permission permission)
    {
        permission.ThrowIfNone(nameof(permission));
        permission.ThrowIfNotSingle(nameof(permission));
        if (IsOwner)
            throw new CannotGrantOwnerPermissionsException();
        if (Permissions.Includes(permission))
            throw new PermissionAlreadyGrantedException();

        Permissions |= permission;
    }
    internal void Revoke(Permission permission)
    {
        permission.ThrowIfNone(nameof(permission));
        permission.ThrowIfNotSingle(nameof(permission));

        if (IsOwner)
            throw new CannotRevokeOwnerPermissionsException();

        if (!Permissions.Includes(permission))
            throw new PermissionNotGrantedException();

        Permissions &= ~permission;
    }
}
