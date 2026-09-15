namespace BudgetManager.Application.Email.Templates;

public sealed record AccountActivationEmailModel(string FirstName, string ActivationUrl, string ExpiresOn);