namespace BudgetManager.Application.Email.Templates;

public sealed record PasswordResetEmailModel(string FirstName, string ResetUrl);