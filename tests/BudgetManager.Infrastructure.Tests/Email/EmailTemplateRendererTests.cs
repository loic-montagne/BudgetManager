using System.Globalization;
using BudgetManager.Application.Email;
using BudgetManager.Application.Email.Templates;
using BudgetManager.Infrastructure.Email;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Email;

public sealed class EmailTemplateRendererTests
{
    private readonly EmailTemplateRenderer _renderer =
        new();

    [Fact]
    public async Task RenderAsync_WhenAccountActivationTemplateExists_RendersHtmlAndTextBodies()
    {
        // Arrange

        var model =
            new AccountActivationEmailModel(
                "Alice",
                "https://example.test/activate?token=abc&user=1",
                "15/09/2026 17:13");

        // Act

        var result =
            await _renderer.RenderAsync(
                EmailTemplates.AccountActivation,
                model,
                CultureInfo.GetCultureInfo("fr-FR"),
                TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(
            result.HtmlBody);

        Assert.NotNull(
            result.TextBody);

        Assert.Contains(
            "Bonjour Alice",
            result.HtmlBody);

        Assert.Contains(
            "cid:budget-manager-banner",
            result.HtmlBody);

        Assert.Contains(
            "https://example.test/activate?token=abc&amp;user=1",
            result.HtmlBody);

        Assert.Contains(
            "https://example.test/activate?token=abc&user=1",
            result.TextBody);

        Assert.Contains(
            "15/09/2026 17:13",
            result.TextBody);

        Assert.DoesNotContain(
            "{{FirstName}}",
            result.HtmlBody);

        Assert.DoesNotContain(
            "{{ActivationUrl}}",
            result.TextBody);

        Assert.DoesNotContain(
            "{{ExpiresOn}}",
            result.TextBody);
    }

    [Fact]
    public async Task RenderAsync_WhenPasswordResetTemplateExists_RendersExpectedBodies()
    {
        var result =
            await _renderer.RenderAsync(
                EmailTemplates.PasswordReset,
                new PasswordResetEmailModel(
                    "Alice",
                    "https://example.test/reset"),
                CultureInfo.GetCultureInfo("fr-FR"),
                TestContext.Current.CancellationToken);

        Assert.Contains(
            "Réinitialisation du mot de passe",
            result.HtmlBody);

        Assert.Contains(
            "cid:budget-manager-banner",
            result.HtmlBody);

        Assert.Contains(
            "Bonjour Alice",
            result.TextBody);

        Assert.Contains(
            "https://example.test/reset",
            result.HtmlBody);

        Assert.Contains(
            "https://example.test/reset",
            result.TextBody);
    }

    [Fact]
    public async Task RenderAsync_WhenHtmlValueContainsMarkup_EncodesHtmlButKeepsPlainTextValue()
    {
        // Arrange

        var firstName =
            "<script>alert('x')</script>";

        var model =
            new AccountActivationEmailModel(
                firstName,
                "https://example.test/activate",
                "15/09/2026 17:13");

        // Act

        var result =
            await _renderer.RenderAsync(
                EmailTemplates.AccountActivation,
                model,
                CultureInfo.GetCultureInfo("fr-FR"),
                TestContext.Current.CancellationToken);

        // Assert

        Assert.DoesNotContain(
            firstName,
            result.HtmlBody);

        Assert.Contains(
            "&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;",
            result.HtmlBody);

        Assert.Contains(
            firstName,
            result.TextBody);
    }

    [Fact]
    public async Task RenderAsync_WhenHtmlValueContainsLineBreaks_ConvertsThemToHtmlBreaks()
    {
        var result =
            await _renderer.RenderAsync(
                EmailTemplates.AccountActivation,
                new AccountActivationEmailModel(
                    $"First{Environment.NewLine}Second",
                    "https://example.test/activate",
                    "15/09/2026 17:13"),
                CultureInfo.GetCultureInfo("fr-FR"),
                TestContext.Current.CancellationToken);

        Assert.Contains(
            "First<br />Second",
            result.HtmlBody);

        Assert.Contains(
            $"First{Environment.NewLine}Second",
            result.TextBody);
    }

    [Fact]
    public async Task RenderAsync_WhenModelDoesNotContainRequiredPlaceholder_Throws()
    {
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _renderer.RenderAsync(
                    EmailTemplates.AccountActivation,
                    new { ActivationUrl = "https://example.test/activate", ExpiresOn = "15/09/2026 17:13" },
                    CultureInfo.GetCultureInfo("fr-FR"),
                TestContext.Current.CancellationToken));

        Assert.Contains(
            "FirstName",
            exception.Message);
    }

    [Fact]
    public async Task RenderAsync_WhenTemplateDoesNotExist_Throws()
    {
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _renderer.RenderAsync(
                    "MissingTemplate",
                    new { Value = "Value" },
                    CultureInfo.GetCultureInfo("fr-FR"),
                TestContext.Current.CancellationToken));

        Assert.Contains(
            "MissingTemplate",
            exception.Message);
    }


    [Fact]
    public async Task RenderAsync_WhenRequestedCultureDoesNotExist_FallsBackToFrenchTemplate()
    {
        // Arrange
        var culture =
            CultureInfo.GetCultureInfo("de-DE");

        var model =
            new PasswordResetEmailModel(
                "Alice",
                "https://example.test/reset");

        // Act
        var result =
            await _renderer.RenderAsync(
                EmailTemplates.PasswordReset,
                model,
                culture,
                TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(
            "Réinitialisation de votre mot de passe",
            result.HtmlBody);

        Assert.Contains(
            "Bonjour Alice",
            result.TextBody);
    }

    [Fact]
    public async Task RenderAsync_WhenRequestedCultureExists_UsesRequestedFrenchTemplate()
    {
        // Arrange
        var culture =
            CultureInfo.GetCultureInfo("fr-FR");

        var model =
            new AccountActivationEmailModel(
                "Alice",
                "https://example.test/activate",
                "15/09/2026 17:13");

        // Act
        var result =
            await _renderer.RenderAsync(
                EmailTemplates.AccountActivation,
                model,
                culture,
                TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(
            "Votre compte Budget Manager a été créé.",
            result.HtmlBody);

        Assert.Contains(
            "Votre compte Budget Manager a été créé.",
            result.TextBody);
    }


    [Fact]
    public async Task RenderAsync_WhenRequestedCultureExists_UsesRequestedEnglishPasswordResetTemplate()
    {
        // Arrange
        var culture =
            CultureInfo.GetCultureInfo("en-US");

        var model =
            new PasswordResetEmailModel(
                "Alice",
                "https://example.test/reset");

        // Act
        var result =
            await _renderer.RenderAsync(
                EmailTemplates.PasswordReset,
                model,
                culture,
                TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(
            "Reset your password",
            result.HtmlBody);

        Assert.Contains(
            "Hello Alice",
            result.TextBody);

        Assert.DoesNotContain(
            "Réinitialisation",
            result.HtmlBody);
    }

    [Fact]
    public async Task RenderAsync_WhenRequestedCultureExists_UsesRequestedEnglishAccountActivationTemplate()
    {
        // Arrange
        var culture =
            CultureInfo.GetCultureInfo("en-US");

        var model =
            new AccountActivationEmailModel(
                "Alice",
                "https://example.test/activate",
                "15/09/2026 17:13");

        // Act
        var result =
            await _renderer.RenderAsync(
                EmailTemplates.AccountActivation,
                model,
                culture,
                TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(
            "Activate your account",
            result.HtmlBody);

        Assert.Contains(
            "Your Budget Manager account has been created.",
            result.TextBody);

        Assert.DoesNotContain(
            "Votre compte Budget Manager",
            result.HtmlBody);
    }


    [Fact]
    public async Task RenderAsync_WhenEmailChangeConfirmationFrenchTemplateExists_RendersExpectedBodies()
    {
        // Arrange
        var model =
            new EmailChangeConfirmationEmailModel(
                "Alice",
                "https://example.test/change-email?token=abc&user=1");

        // Act
        var result =
            await _renderer.RenderAsync(
                EmailTemplates.EmailChangeConfirmation,
                model,
                CultureInfo.GetCultureInfo("fr-FR"),
                TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(
            result.HtmlBody);

        Assert.NotNull(
            result.TextBody);

        Assert.Contains(
            "Confirmation de votre nouvelle adresse mail",
            result.HtmlBody);

        Assert.Contains(
            "Bonjour Alice",
            result.TextBody);

        Assert.Contains(
            "cid:budget-manager-banner",
            result.HtmlBody);

        Assert.Contains(
            "https://example.test/change-email?token=abc&amp;user=1",
            result.HtmlBody);

        Assert.Contains(
            "https://example.test/change-email?token=abc&user=1",
            result.TextBody);

        Assert.DoesNotContain(
            "{{FirstName}}",
            result.HtmlBody);

        Assert.DoesNotContain(
            "{{ActivationUrl}}",
            result.TextBody);
    }

    [Fact]
    public async Task RenderAsync_WhenEmailChangeConfirmationEnglishTemplateExists_UsesEnglishTemplate()
    {
        // Arrange
        var model =
            new EmailChangeConfirmationEmailModel(
                "Alice",
                "https://example.test/change-email");

        // Act
        var result =
            await _renderer.RenderAsync(
                EmailTemplates.EmailChangeConfirmation,
                model,
                CultureInfo.GetCultureInfo("en-US"),
                TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(
            "Confirm your new email address",
            result.HtmlBody);

        Assert.Contains(
            "Hello Alice",
            result.TextBody);

        Assert.DoesNotContain(
            "Confirmez votre nouvelle adresse mail",
            result.HtmlBody);
    }

    [Fact]
    public async Task RenderAsync_WhenHtmlTemplatesAreRendered_UseOutlookCompatibleBannerAndButtonMarkup()
    {
        await AssertOutlookCompatibleHtmlAsync(
            EmailTemplates.AccountActivation,
            new AccountActivationEmailModel(
                "Alice",
                "https://example.test/activate?token=abc&user=1",
                "15/09/2026 17:13"),
            "fr-FR");

        await AssertOutlookCompatibleHtmlAsync(
            EmailTemplates.AccountActivation,
            new AccountActivationEmailModel(
                "Alice",
                "https://example.test/activate?token=abc&user=1",
                "09/15/2026 5:13 PM"),
            "en-US");

        await AssertOutlookCompatibleHtmlAsync(
            EmailTemplates.EmailChangeConfirmation,
            new EmailChangeConfirmationEmailModel(
                "Alice",
                "https://example.test/change-email?token=abc&user=1"),
            "fr-FR");

        await AssertOutlookCompatibleHtmlAsync(
            EmailTemplates.EmailChangeConfirmation,
            new EmailChangeConfirmationEmailModel(
                "Alice",
                "https://example.test/change-email?token=abc&user=1"),
            "en-US");

        await AssertOutlookCompatibleHtmlAsync(
            EmailTemplates.PasswordReset,
            new PasswordResetEmailModel(
                "Alice",
                "https://example.test/reset?token=abc&user=1"),
            "fr-FR");

        await AssertOutlookCompatibleHtmlAsync(
            EmailTemplates.PasswordReset,
            new PasswordResetEmailModel(
                "Alice",
                "https://example.test/reset?token=abc&user=1"),
            "en-US");
    }

    [Fact]
    public async Task RenderAsync_WhenTemplateNameIsEmpty_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _renderer.RenderAsync(
                string.Empty,
                new { Value = "Value" },
                CultureInfo.GetCultureInfo("fr-FR"),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RenderAsync_WhenModelIsNull_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _renderer.RenderAsync<object>(
                EmailTemplates.AccountActivation,
                null!,
                CultureInfo.GetCultureInfo("fr-FR"),
                TestContext.Current.CancellationToken));
    }

    private async Task AssertOutlookCompatibleHtmlAsync<TModel>(
        string templateName,
        TModel model,
        string cultureName)
    {
        var result =
            await _renderer.RenderAsync(
                templateName,
                model,
                CultureInfo.GetCultureInfo(cultureName),
                TestContext.Current.CancellationToken);

        Assert.NotNull(result.HtmlBody);

        Assert.Contains(
            "height=\"132\"",
            result.HtmlBody);

        Assert.Contains(
            "mso-line-height-rule:exactly",
            result.HtmlBody);

        Assert.Contains(
            "<!--[if mso]>",
            result.HtmlBody);

        Assert.Contains(
            "<v:roundrect",
            result.HtmlBody);

        Assert.Contains(
            "xmlns:w=\"urn:schemas-microsoft-com:office:word\"",
            result.HtmlBody);

        Assert.Contains(
            "mso-hide:all",
            result.HtmlBody);

        Assert.Contains(
            "mso-style-textfill-type:gradient",
            result.HtmlBody);

        Assert.Contains(
            "class=\"keep-white\"",
            result.HtmlBody);

        Assert.Contains(
            "background:rgba(8,183,167,1)",
            result.HtmlBody);

        Assert.Contains(
            "class=\"email-card\"",
            result.HtmlBody);

        Assert.Contains(
            "border:none !important",
            result.HtmlBody);

        Assert.Contains(
            "class=\"email-card-section\"",
            result.HtmlBody);

        Assert.Contains(
            "class=\"email-card-section email-card-footer\"",
            result.HtmlBody);

        Assert.Contains(
            ".email-card-footer",
            result.HtmlBody);

        Assert.Contains(
            "border-bottom:1px solid #D8E5E7 !important",
            result.HtmlBody);

        Assert.DoesNotContain(
            "width=\"578\" height=\"3\"",
            result.HtmlBody);

        Assert.DoesNotContain(
            "bgcolor=\"#08B7A7\"",
            result.HtmlBody);
    }

}
