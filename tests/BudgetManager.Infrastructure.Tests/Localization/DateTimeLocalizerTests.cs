using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Infrastructure.Localization;
using Microsoft.Extensions.Options;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Localization;

public sealed class DateTimeLocalizerTests
{
    [Theory]
    [InlineData(2026, 1, 15, 12, 13, 1)]
    [InlineData(2026, 7, 15, 12, 14, 2)]
    public void ToLocalTime_WhenEuropeParis_UsesExpectedSeasonalOffset(
        int year,
        int month,
        int day,
        int utcHour,
        int expectedHour,
        int expectedOffsetHours)
    {
        // Arrange

        var localizer =
            new DateTimeLocalizer(
                Options.Create(
                    new LocalizationOptions
                    {
                        TimeZone = "Europe/Paris"
                    }));

        var utcValue =
            new DateTimeOffset(
                year,
                month,
                day,
                utcHour,
                0,
                0,
                TimeSpan.Zero);

        // Act

        var localValue =
            localizer.ToLocalTime(
                utcValue);

        // Assert

        Assert.Equal(
            expectedHour,
            localValue.Hour);

        Assert.Equal(
            TimeSpan.FromHours(expectedOffsetHours),
            localValue.Offset);
    }
}
