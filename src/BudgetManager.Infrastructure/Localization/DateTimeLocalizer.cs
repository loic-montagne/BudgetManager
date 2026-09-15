using BudgetManager.Application.Abstractions.Localization;
using BudgetManager.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace BudgetManager.Infrastructure.Localization;

internal sealed class DateTimeLocalizer : IDateTimeLocalizer
{
    private readonly TimeZoneInfo _timeZone;

    public DateTimeLocalizer(IOptions<LocalizationOptions> options)
    {
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            options.Value.TimeZone);
    }

    public DateTimeOffset ToLocalTime(DateTimeOffset value)
    {
        return TimeZoneInfo.ConvertTime(value, _timeZone);
    }
}
