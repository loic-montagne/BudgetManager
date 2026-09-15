namespace BudgetManager.Application.Enums;

/// <summary>
/// Describes whether the current user can lock a budget.
/// </summary>
public enum BudgetLockableStatus
{
    /// <summary>The budget can be locked.</summary>
    Lockable,
    /// <summary>The current user does not have lock permission.</summary>
    NotAuthorized,
    /// <summary>The budget is already locked.</summary>
    AlreadyLocked
}
