namespace BudgetManager.Application.Abstractions.Localization;

public interface IDateTimeLocalizer
{
    DateTimeOffset ToLocalTime(DateTimeOffset value);
}
