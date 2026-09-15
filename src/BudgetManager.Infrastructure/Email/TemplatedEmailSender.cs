using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Email;
using System.Globalization;

namespace BudgetManager.Infrastructure.Email;

internal sealed class TemplatedEmailSender(IEmailTemplateRenderer templateRenderer, IEmailSender emailSender) : ITemplatedEmailSender
{
    private const string BannerContentId = "budget-manager-banner";
    private const string BannerResourceName = "BudgetManager.Infrastructure.Email.Assets.budget-manager-banner.png";

    public async Task SendAsync<TModel>(TemplatedEmailMessage message, TModel model, CultureInfo culture, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var emailMessage = await message.RenderEmailMessageAsync(templateRenderer, model, culture, cancellationToken);

        await using var bannerStream = AddBannerIfRequired(emailMessage);

        await emailSender.SendAsync(emailMessage, cancellationToken);
    }

    private static Stream? AddBannerIfRequired(EmailMessage message)
    {
        if (message.HtmlBody is null ||
            !message.HtmlBody.Contains($"cid:{BannerContentId}", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var stream = typeof(TemplatedEmailSender).Assembly.GetManifestResourceStream(BannerResourceName)
            ?? throw new InvalidOperationException($"Embedded email banner '{BannerResourceName}' was not found.");

        message.InlineImages.Add(
            new EmailInlineImage(
                BannerContentId,
                stream,
                "image/png"));

        return stream;
    }
}
