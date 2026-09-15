namespace BudgetManager.Infrastructure.Configuration;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public required Uri PublicUrl { get; init; }
}
