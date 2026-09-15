using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Email;
using BudgetManager.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BudgetManager.Infrastructure.Email.Smtp;

internal sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.To.Count == 0 && message.Cc.Count == 0 && message.Bcc.Count == 0)
            throw new ArgumentException("Email must have at least one recipient.", nameof(message));
        if (string.IsNullOrWhiteSpace(message.Subject))
            throw new ArgumentException("Email subject is required.", nameof(message));
        if (message.IsBodyEmpty)
            throw new ArgumentException("Email must have a text or HTML body.", nameof(message));

        using var mimeMessage = CreateMessage(message);

        using var client = new SmtpClient
        {
            Timeout = _options.Timeout
        };

        try
        {
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                _options.SecureOptions,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                await client.AuthenticateAsync(
                    _options.UserName,
                    _options.Password,
                    cancellationToken);
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            logger.LogInformation("Email {Subject} sent to {RecipientCount} recipient(s).", message.Subject, message.To.Count + message.Cc.Count + message.Bcc.Count);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to send email {Subject}.", message.Subject);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                try
                {
                    await client.DisconnectAsync(true, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "An error occurred while disconnecting from the SMTP server.");
                }
            }
        }
    }

    private MimeMessage CreateMessage(EmailMessage email)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));

        AddAddresses(message.To, email.To);

        AddAddresses(message.Cc, email.Cc);

        AddAddresses(message.Bcc, email.Bcc);

        message.Subject = email.Subject;

        var bodyBuilder =
            new BodyBuilder
            {
                TextBody = email.TextBody,
                HtmlBody = email.HtmlBody
            };

        AddAttachments(bodyBuilder, email.Attachments);

        AddInlineImages(bodyBuilder, email.InlineImages);

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    private static void AddAddresses(InternetAddressList target, IEnumerable<EmailAddress> addresses)
    {
        foreach (var address in addresses)
        {
            target.Add(new MailboxAddress(address.DisplayName, address.Address));
        }
    }

    private static void AddAttachments(BodyBuilder builder, IEnumerable<EmailAttachment> attachments)
    {
        foreach (var attachment in attachments)
        {
            builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        }
    }

    private static void AddInlineImages(BodyBuilder builder, IEnumerable<EmailInlineImage> images)
    {
        foreach (var image in images)
        {
            var resource =
                builder.LinkedResources.Add(
                    image.ContentId,
                    image.Content,
                    ContentType.Parse(
                        image.ContentType));

            resource.ContentId =
                image.ContentId;
        }
    }
}
