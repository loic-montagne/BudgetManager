namespace BudgetManager.Application.Email;

public sealed record EmailAddress(
    string Address,
    string? DisplayName = null);
