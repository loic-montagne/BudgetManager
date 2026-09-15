namespace BudgetManager.Application.Common.Errors;

public sealed record ValidationError(string PropertyName, string ErrorMessage, string? ErrorCode);
