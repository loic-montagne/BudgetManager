namespace BudgetManager.Domain.Enums;

/// <summary>
/// Defines the permissions that can be granted on a budget.
/// </summary>
[Flags]
public enum Permission
{
    /// <summary>No permission is granted.</summary>
    None = 0,

    /// <summary>Allows the budget to be viewed.</summary>
    View = 1 << 0,

    /// <summary>Allows the budget and its contents to be modified.</summary>
    Edit = 1 << 1,

    /// <summary>Allows the budget to be deleted.</summary>
    Delete = 1 << 2,

    /// <summary>Allows budget access to be managed.</summary>
    Share = 1 << 3,

    /// <summary>Allows the budget to be locked and unlocked.</summary>
    Lock = 1 << 4,

    /// <summary>Includes every available budget permission.</summary>
    All = View | Edit | Delete | Share | Lock
}
