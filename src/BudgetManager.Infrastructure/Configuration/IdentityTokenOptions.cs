namespace BudgetManager.Infrastructure.Configuration;

public sealed class IdentityTokenOptions
{
    public const string SectionName = "IdentityTokens";

    public required TimeSpan AccountActivationLifetime { get; init; }
    public required TimeSpan EmailChangeLifetime { get; init; }
    public required TimeSpan PasswordResetLifetime { get; init; }
}
