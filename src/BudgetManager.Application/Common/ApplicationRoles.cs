namespace BudgetManager.Application.Common;

public static class ApplicationRoles
{
    public const string Administrator = "Administrator";
    public const string User = "User";

    public static readonly IReadOnlyCollection<string> All = [Administrator, User];
}
