namespace BudgetManager.Application.Common;

public static class SupportedThemes
{
    public const string Light = "light";
    public const string Dark = "dark";
    public const string System = "system";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Light,
            Dark,
            System
        };

    public static readonly IReadOnlyDictionary<string, string> AllWithKeys =
        new Dictionary<string, string>([
            new KeyValuePair<string, string>(nameof(System), System),
            new KeyValuePair<string, string>(nameof(Light), Light),
            new KeyValuePair<string, string>(nameof(Dark), Dark)
            ]);
}
