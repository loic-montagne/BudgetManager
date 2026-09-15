namespace BudgetManager.Application.Enums;

/// <summary>
/// Describes whether the current user can unlock a budget.
/// </summary>
public enum BudgetUnlockableStatus
{
    /// <summary>The budget can be unlocked.</summary>
    Unlockable,
    /// <summary>The current user does not have lock permission.</summary>
    NotAuthorized,
    /// <summary>The budget is already unlocked.</summary>
    AlreadyUnlocked
}
