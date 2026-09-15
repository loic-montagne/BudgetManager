using BudgetManager.Application.Common;

namespace BudgetManager.Infrastructure.Configuration;

internal sealed class UsersSeedOptions
{
    public const string SectionName = "Seed:Users";
    public bool Enabled { get; init; }
    public Dictionary<string, UserSeedOptions> Users { get; init; } = [];
}

internal sealed class UserSeedOptions
{
    public bool Enabled { get; init; }

    public string? Role { get; init; }

    public string? Email { get; init; }

    public string? Password { get; init; }

    public string? LastName { get; init; }

    public string? FirstName { get; init; }

    public void EnsureIsConfigured()
    {
        if (!Enabled)
            return;

        if (string.IsNullOrWhiteSpace(Role))
            throw new InvalidOperationException("User seed role is not configured.");
        if (!ApplicationRoles.All.Contains(Role))
            throw new InvalidOperationException($"User seed role is not recognized. You must configure a role in: {string.Join(", ", ApplicationRoles.All)}");

        if (string.IsNullOrWhiteSpace(Email))
            throw new InvalidOperationException("User seed email is not configured.");

        if (string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("User seed password is not configured.");

        if (string.IsNullOrWhiteSpace(LastName))
            throw new InvalidOperationException("User seed last name is not configured.");

        if (string.IsNullOrWhiteSpace(FirstName))
            throw new InvalidOperationException("User seed first name is not configured.");
    }
}