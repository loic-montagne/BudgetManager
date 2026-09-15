using BudgetManager.Domain.ValueObjects;
using System.Globalization;
using Xunit;

namespace BudgetManager.Domain.Tests.ValueObjects;

public sealed class UDecimalTests
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    private static readonly CultureInfo FrenchCulture = CultureInfo.GetCultureInfo("fr-FR");

    [Fact]
    public void Constants_HaveExpectedValues()
    {
        Assert.Equal(0m, UDecimal.MinValue.ToDecimal());
        Assert.Equal(decimal.MaxValue, UDecimal.MaxValue.ToDecimal());
        Assert.Equal(1m, UDecimal.One.ToDecimal());
        Assert.Equal(0m, UDecimal.Zero.ToDecimal());
    }

    [Fact]
    public void Constructor_WithPositiveOrZeroValue_CreatesValue()
    {
        Assert.Equal(0m, new UDecimal(0m).ToDecimal());
        Assert.Equal(12.5m, new UDecimal(12.5m).ToDecimal());
    }

    [Fact]
    public void Constructor_WithNegativeValue_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new UDecimal(-0.01m));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void ImplicitConversions_RoundTripValue()
    {
        UDecimal unsignedDecimal = 12.5m;
        decimal decimalValue = unsignedDecimal;

        Assert.Equal(12.5m, decimalValue);
    }

    [Fact]
    public void ComparisonOperators_ReturnExpectedResults()
    {
        UDecimal smaller = 1m;
        UDecimal equal = 1m;
        UDecimal larger = 2m;

        Assert.True(smaller < larger);
        Assert.False(larger < smaller);

        Assert.True(larger > smaller);
        Assert.False(smaller > larger);

        Assert.True(smaller <= equal);
        Assert.True(smaller <= larger);
        Assert.False(larger <= smaller);

        Assert.True(larger >= equal);
        Assert.True(larger >= smaller);
        Assert.False(smaller >= larger);

        Assert.True(smaller == equal);
        Assert.False(smaller == larger);

        Assert.True(smaller != larger);
        Assert.False(smaller != equal);
    }

    [Fact]
    public void ArithmeticOperators_ProduceExpectedValues()
    {
        UDecimal left = 12.5m;
        UDecimal right = 2.5m;

        Assert.Equal(15m, (left + right).ToDecimal());
        Assert.Equal(10m, (left - right).ToDecimal());
        Assert.Equal(31.25m, (left * right).ToDecimal());
        Assert.Equal(5m, (left / right).ToDecimal());
    }

    [Fact]
    public void Subtraction_WhenResultWouldBeNegative_Throws()
    {
        UDecimal smaller = 1m;
        UDecimal larger = 2m;

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = smaller - larger);
    }

    [Fact]
    public void Division_ByZero_Throws()
    {
        UDecimal value = 10m;

        Assert.Throws<DivideByZeroException>(() => _ = value / UDecimal.Zero);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        UDecimal value = 12.5m;
        UDecimal sameValue = 12.5m;

        Assert.True(value.Equals(sameValue));
        Assert.True(value.Equals((object)sameValue));
        Assert.Equal(value.GetHashCode(), sameValue.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValueOrType_ReturnsFalse()
    {
        UDecimal value = 12.5m;

        Assert.False(value.Equals((UDecimal)13m));
        Assert.False(value.Equals((object)13m));
        Assert.False(value.Equals(null));
    }

    [Theory]
    [InlineData("12.50", "fr-FR", "12.50")]
    [InlineData("12,50", "fr-FR", "12.50")]
    [InlineData("12.50", "en-US", "12.50")]
    public void Parse_String_AcceptsDotOrCultureDecimalSeparator(
        string input,
        string cultureName,
        string expectedValue)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);

        var result = UDecimal.Parse(input, culture);

        Assert.Equal(
            decimal.Parse(expectedValue, InvariantCulture),
            result.ToDecimal());
    }

    [Fact]
    public void Parse_String_WithNullProvider_UsesCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = FrenchCulture;

            var result = UDecimal.Parse("12.50", provider: null);

            Assert.Equal(12.5m, result.ToDecimal());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Parse_String_WithInvalidValue_Throws()
    {
        Assert.Throws<FormatException>(
            () => UDecimal.Parse("not-a-number", InvariantCulture));
    }

    [Fact]
    public void Parse_String_WithNegativeValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => UDecimal.Parse("-1", InvariantCulture));
    }

    [Fact]
    public void Parse_Span_ReturnsExpectedValue()
    {
        var result = UDecimal.Parse("12.50".AsSpan(), FrenchCulture);

        Assert.Equal(12.5m, result.ToDecimal());
    }

    [Fact]
    public void Parse_Span_WithInvalidValue_Throws()
    {
        Assert.Throws<FormatException>(
            () => UDecimal.Parse("invalid".AsSpan(), InvariantCulture));
    }

    [Fact]
    public void TryParse_String_WithValidValue_ReturnsTrue()
    {
        var success = UDecimal.TryParse("12.50", FrenchCulture, out var result);

        Assert.True(success);
        Assert.Equal(12.5m, result.ToDecimal());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    public void TryParse_String_WithInvalidValue_ReturnsFalse(string? input)
    {
        var success = UDecimal.TryParse(input, InvariantCulture, out var result);

        Assert.False(success);
        Assert.Equal(UDecimal.Zero, result);
    }

    [Fact]
    public void TryParse_String_WithNegativeValue_ReturnsFalse()
    {
        var success = UDecimal.TryParse(
            "-10",
            CultureInfo.InvariantCulture,
            out var result);

        Assert.False(success);
        Assert.Equal(UDecimal.Zero, result);
    }

    [Fact]
    public void TryParse_Span_WithValidValue_ReturnsTrue()
    {
        var success = UDecimal.TryParse(
            "12.50".AsSpan(),
            FrenchCulture,
            out var result);

        Assert.True(success);
        Assert.Equal(12.5m, result.ToDecimal());
    }

    [Fact]
    public void TryParse_Span_WithInvalidValue_ReturnsFalse()
    {
        var success = UDecimal.TryParse(
            "invalid".AsSpan(),
            InvariantCulture,
            out var result);

        Assert.False(success);
        Assert.Equal(UDecimal.Zero, result);
    }

    [Fact]
    public void TryParse_Span_WithNegativeValue_ReturnsFalse()
    {
        var success = UDecimal.TryParse(
            "-10".AsSpan(),
            CultureInfo.InvariantCulture,
            out var result);

        Assert.False(success);
        Assert.Equal(UDecimal.Zero, result);
    }

    [Fact]
    public void TryFormat_WithSufficientDestination_WritesFormattedValue()
    {
        UDecimal value = 12.5m;
        Span<char> destination = stackalloc char[16];

        var success = value.TryFormat(
            destination,
            out var charsWritten,
            "F2".AsSpan(),
            InvariantCulture);

        Assert.True(success);
        Assert.Equal("12.50", destination[..charsWritten].ToString());
    }

    [Fact]
    public void TryFormat_WithInsufficientDestination_ReturnsFalse()
    {
        UDecimal value = 12.5m;
        Span<char> destination = stackalloc char[2];

        var success = value.TryFormat(
            destination,
            out var charsWritten,
            "F2".AsSpan(),
            InvariantCulture);

        Assert.False(success);
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void ToString_WithoutArguments_UsesCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = FrenchCulture;
            UDecimal value = 12.5m;

            Assert.Equal(12.5m.ToString(FrenchCulture), value.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void ToString_WithProvider_UsesProvider()
    {
        UDecimal value = 12.5m;

        Assert.Equal("12.5", value.ToString(InvariantCulture));
        Assert.Equal("12,5", value.ToString(FrenchCulture));
    }

    [Fact]
    public void ToString_WithFormat_UsesCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = FrenchCulture;
            UDecimal value = 12.5m;

            Assert.Equal("12,50", value.ToString("F2"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void ToString_WithFormatAndProvider_UsesBoth()
    {
        UDecimal value = 12.5m;

        Assert.Equal("12.50", value.ToString("F2", InvariantCulture));
        Assert.Equal("12,50", value.ToString("F2", FrenchCulture));
    }
}