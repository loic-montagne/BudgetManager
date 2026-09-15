using System.Globalization;
using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Email;
using NSubstitute;
using Xunit;

namespace BudgetManager.Application.Tests.Email;

public sealed class TemplatedEmailMessageTests
{
    [Fact]
    public async Task RenderEmailMessageAsync_WhenTemplateIsRendered_CopiesMetadataAndRenderedBodies()
    {
        // Arrange

        var renderer =
            Substitute.For<IEmailTemplateRenderer>();

        var model =
            new TestTemplateModel(
                "Value");

        var culture =
            CultureInfo.GetCultureInfo("en-US");

        renderer
            .RenderAsync(
                "Template",
                model,
                culture,
                TestContext.Current.CancellationToken)
            .Returns(
                new RenderedEmailTemplate(
                    "Text body",
                    "<p>HTML body</p>"));

        using var attachmentStream =
            new MemoryStream([1, 2, 3]);

        using var imageStream =
            new MemoryStream([4, 5, 6]);

        var recipient =
            new EmailAddress(
                "to@example.test",
                "To");

        var cc =
            new EmailAddress(
                "cc@example.test");

        var bcc =
            new EmailAddress(
                "bcc@example.test");

        var attachment =
            new EmailAttachment(
                "test.txt",
                attachmentStream,
                "text/plain");

        var inlineImage =
            new EmailInlineImage(
                "logo",
                imageStream,
                "image/png");

        var message =
            new TemplatedEmailMessage
            {
                Subject = "Subject",
                TemplateName = "Template"
            };

        message.To.Add(recipient);
        message.Cc.Add(cc);
        message.Bcc.Add(bcc);
        message.Attachments.Add(attachment);
        message.InlineImages.Add(inlineImage);

        // Act

        var result =
            await message.RenderEmailMessageAsync(
                renderer,
                model,
                culture,
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Equal(
            "Subject",
            result.Subject);

        Assert.Equal(
            "Text body",
            result.TextBody);

        Assert.Equal(
            "<p>HTML body</p>",
            result.HtmlBody);

        Assert.Equal(
            [recipient],
            result.To);

        Assert.Equal(
            [cc],
            result.Cc);

        Assert.Equal(
            [bcc],
            result.Bcc);

        Assert.Equal(
            [attachment],
            result.Attachments);

        Assert.Equal(
            [inlineImage],
            result.InlineImages);

        await renderer
            .Received(1)
            .RenderAsync(
                "Template",
                model,
                culture,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RenderEmailMessageAsync_WhenRendered_CollectionsAreIndependentFromSourceMessage()
    {
        // Arrange

        var renderer =
            Substitute.For<IEmailTemplateRenderer>();

        var model =
            new TestTemplateModel(
                "Value");

        var culture =
            CultureInfo.GetCultureInfo("fr-FR");

        renderer
            .RenderAsync(
                "Template",
                model,
                culture,
                TestContext.Current.CancellationToken)
            .Returns(
                new RenderedEmailTemplate(
                    "Text",
                    "HTML"));

        var originalRecipient =
            new EmailAddress(
                "original@example.test");

        var message =
            new TemplatedEmailMessage
            {
                Subject = "Subject",
                TemplateName = "Template"
            };

        message.To.Add(
            originalRecipient);

        // Act

        var result =
            await message.RenderEmailMessageAsync(
                renderer,
                model,
                culture,
                TestContext.Current.CancellationToken);

        message.To.Add(
            new EmailAddress(
                "later@example.test"));

        result.Cc.Add(
            new EmailAddress(
                "result@example.test"));

        // Assert

        Assert.Single(
            result.To,
            x => x == originalRecipient);

        Assert.Empty(
            message.Cc);
    }


    [Fact]
    public async Task RenderEmailMessageAsync_WhenCultureIsProvided_ForwardsCultureToRenderer()
    {
        // Arrange
        var renderer =
            Substitute.For<IEmailTemplateRenderer>();

        var model =
            new TestTemplateModel("Value");

        var culture =
            CultureInfo.GetCultureInfo("en-US");

        renderer
            .RenderAsync(
                "Template",
                model,
                culture,
                TestContext.Current.CancellationToken)
            .Returns(
                new RenderedEmailTemplate(
                    "Text",
                    "HTML"));

        var message =
            new TemplatedEmailMessage
            {
                Subject = "Subject",
                TemplateName = "Template"
            };

        // Act
        await message.RenderEmailMessageAsync(
            renderer,
            model,
            culture,
            TestContext.Current.CancellationToken);

        // Assert
        await renderer
            .Received(1)
            .RenderAsync(
                "Template",
                model,
                culture,
                TestContext.Current.CancellationToken);
    }

    private sealed record TestTemplateModel(
        string Value);
}
