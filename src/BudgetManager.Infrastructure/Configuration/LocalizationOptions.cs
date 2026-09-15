namespace BudgetManager.Infrastructure.Configuration;

public sealed class LocalizationOptions
{
    public const string SectionName = "Localization";

    public string TimeZone { get; init; } = "UTC";
}
