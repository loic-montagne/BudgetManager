using BudgetManager.Domain.Extensions;
using Xunit;

namespace BudgetManager.Domain.Tests.Extensions;

public sealed class EnumExtensionsTests
{
    public enum SampleValue
    {
        None = 0,
        First = 1,
        Second = 2
    }

    [Theory]
    [InlineData(SampleValue.None)]
    [InlineData(SampleValue.First)]
    [InlineData(SampleValue.Second)]
    public void IsValid_WhenValueIsDefined_ReturnsTrue(
        SampleValue value)
    {
        Assert.True(
            value.IsValid());
    }

    [Fact]
    public void IsValid_WhenValueIsUndefined_ReturnsFalse()
    {
        var value =
            (SampleValue)999;

        Assert.False(
            value.IsValid());
    }

    [Theory]
    [InlineData(SampleValue.None)]
    [InlineData(SampleValue.First)]
    [InlineData(SampleValue.Second)]
    public void ThrowIfNotValid_WhenValueIsDefined_DoesNotThrow(
        SampleValue value)
    {
        var exception =
            Record.Exception(
                () => value.ThrowIfNotValid());

        Assert.Null(
            exception);
    }

    [Fact]
    public void ThrowIfNotValid_WhenValueIsUndefined_ThrowsWithExpectedParameterName()
    {
        var value =
            (SampleValue)999;

        var exception =
            Assert.Throws<ArgumentException>(
                () => value.ThrowIfNotValid(
                    "value"));

        Assert.Equal(
            "value",
            exception.ParamName);
    }
}
