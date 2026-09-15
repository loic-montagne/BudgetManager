using BudgetManager.Application.Abstractions.Email;
using System.Globalization;

namespace BudgetManager.Application.Email;

public sealed class TemplatedEmailMessage : EmailMessageBase
{
    public required string TemplateName { get; init; }

    public async Task<EmailMessage> RenderEmailMessageAsync<TModel>(IEmailTemplateRenderer templateRenderer, TModel model, CultureInfo culture, CancellationToken cancellationToken)
    {
        var template = await templateRenderer.RenderAsync(TemplateName, model, culture, cancellationToken);

        return new EmailMessage
        {
            Subject = Subject,
            To = [.. To],
            Cc = [.. Cc],
            Bcc = [.. Bcc],
            Attachments = [.. Attachments],
            InlineImages = [.. InlineImages],
            TextBody = template.TextBody,
            HtmlBody = template.HtmlBody
        };
    }
}
