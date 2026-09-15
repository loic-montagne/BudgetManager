using BudgetManager.Application.Email;
using Xunit;

namespace BudgetManager.Application.Tests.Email;

public sealed class EmailMessageTests
{
    [Fact]
    public void IsBodyEmpty_WhenBothBodiesAreNull_ReturnsTrue()
    {
        var message =
            new EmailMessage
            {
                Subject = "Subject"
            };

        Assert.True(
            message.IsBodyEmpty);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "\t")]
    public void IsBodyEmpty_WhenBothBodiesAreWhitespace_ReturnsTrue(
        string textBody,
        string htmlBody)
    {
        var message =
            new EmailMessage
            {
                Subject = "Subject",
                TextBody = textBody,
                HtmlBody = htmlBody
            };

        Assert.True(
            message.IsBodyEmpty);
    }

    [Theory]
    [InlineData("Text", null)]
    [InlineData(null, "<p>HTML</p>")]
    [InlineData("Text", "<p>HTML</p>")]
    public void IsBodyEmpty_WhenAtLeastOneBodyHasContent_ReturnsFalse(
        string? textBody,
        string? htmlBody)
    {
        var message =
            new EmailMessage
            {
                Subject = "Subject",
                TextBody = textBody,
                HtmlBody = htmlBody
            };

        Assert.False(
            message.IsBodyEmpty);
    }
}
