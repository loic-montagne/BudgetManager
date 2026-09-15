namespace BudgetManager.Application.Enums;

/// <summary>
/// Describes whether the current user can modify a budget.
/// </summary>
public enum BudgetEditableStatus
{
    /// <summary>The budget can be modified.</summary>
    Editable,
    /// <summary>The current user does not have edit permission.</summary>
    NotAuthorized,
    /// <summary>The budget is locked.</summary>
    Locked
}
