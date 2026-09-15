using BudgetManager.Infrastructure.Identity;

namespace BudgetManager.Infrastructure.Extensions;

internal static class ApplicationUserExtensions
{
    public static string GetDisplayName(this ApplicationUser? user, Guid fallbackId)
    {
        if (user is null)
            return fallbackId == Guid.Empty
                ? string.Empty
                : fallbackId.ToString();

        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        if (!string.IsNullOrWhiteSpace(user.Email))
            return user.Email;

        return fallbackId == Guid.Empty
            ? string.Empty
            : fallbackId.ToString();
    }
}
