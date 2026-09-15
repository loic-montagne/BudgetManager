using BudgetManager.Infrastructure.Email;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Email;

public sealed class HtmlEncoderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EncodeWithLineBreaks_WhenValueIsNullOrEmpty_ReturnsEmpty(
        string? value)
    {
        Assert.Equal(
            string.Empty,
            HtmlEncoder.EncodeWithLineBreaks(
                value));
    }

    [Fact]
    public void EncodeWithLineBreaks_WhenValueContainsHtml_EncodesMarkup()
    {
        var result =
            HtmlEncoder.EncodeWithLineBreaks(
                "<strong>A & B</strong>");

        Assert.Equal(
            "&lt;strong&gt;A &amp; B&lt;/strong&gt;",
            result);
    }

    [Fact]
    public void EncodeWithLineBreaks_WhenValueContainsEnvironmentLineBreak_ReplacesItWithHtmlBreak()
    {
        var result =
            HtmlEncoder.EncodeWithLineBreaks(
                $"First{Environment.NewLine}Second");

        Assert.Equal(
            "First<br />Second",
            result);
    }
}
