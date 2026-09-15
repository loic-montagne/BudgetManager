namespace BudgetManager.Application.Common;

public static class SupportedCultures
{
    public const string Default = French;

    public const string French = "fr-FR";
    public const string English = "en-US";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            French,
            English
        };
    public static readonly IReadOnlyDictionary<string, string> AllWithKeys =
        new Dictionary<string, string>([
            new KeyValuePair<string, string>(nameof(French), French),
            new KeyValuePair<string, string>(nameof(English), English)
            ]);
}
