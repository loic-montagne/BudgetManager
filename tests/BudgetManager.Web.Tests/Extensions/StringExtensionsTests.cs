using BudgetManager.Web.Common;
using BudgetManager.Web.Extensions;
using Xunit;

namespace BudgetManager.Web.Tests.Extensions;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData(null, 7, 7)]
    [InlineData("", 7, 7)]
    [InlineData(" ", 7, 7)]
    [InlineData("abc", 7, 7)]
    [InlineData("42", 7, 42)]
    public void ToInt_WithInput_ReturnsExpected(string? value, int defaultValue, int expected)
        => Assert.Equal(expected, value.ToInt(defaultValue));

    [Fact]
    public void ToHtml_WithSpecialCharactersAndLineBreaks_EncodesAndFormats()
        => Assert.Equal("&lt;b&gt;&amp;&lt;/b&gt;<br/>\r\nline&nbsp;&nbsp;2", "<b>&</b>\nline  2".ToHtml());

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("BankController", "Bank")]
    [InlineData("ControllerBank", "ControllerBank")]
    public void ToControllerName_WithInput_ReturnsExpected(string? value, string expected)
        => Assert.Equal(expected, value.ToControllerName());

    [Fact]
    public void ToEnumerable_WithSeparatedValues_RemovesEmptyValues()
        => Assert.Equal(["a", "b"], $"a{Constants.SeparatorChar}{Constants.SeparatorChar}b".ToEnumerable());

    [Fact]
    public void ToEnumerableGeneric_WithSpecialValues_SetsFlagsAndConvertsRegularValues()
    {
        var value = $"{Constants.NoneValue}{Constants.SeparatorChar}12{Constants.SeparatorChar}{Constants.AllValue}";
        var result = value.ToEnumerable(int.Parse, out var none, out var all).ToArray();
        Assert.True(none);
        Assert.True(all);
        Assert.Equal([12], result);
    }

    [Fact]
    public void ToEnumerableGeneric_WithNullConverter_Throws()
        => Assert.Throws<ArgumentNullException>(() => "1".ToEnumerable<int>(null!, out _, out _).ToArray());
}
