using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Email;
using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Infrastructure.Email;
using BudgetManager.Infrastructure.Email.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Email;

public sealed class EmailSenderTests
{
    [Fact]
    public async Task TemplatedEmailSender_WhenMessageIsNull_Throws()
    {
        var renderer =
            Substitute.For<IEmailTemplateRenderer>();

        var sender =
            Substitute.For<IEmailSender>();

        var service =
            new TemplatedEmailSender(
                renderer,
                sender);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.SendAsync(
                null!,
                new { Value = "Value" },
                CultureInfo.GetCultureInfo("fr-FR"),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TemplatedEmailSender_WhenMessageIsValid_RendersAndSendsExpectedMessage()
    {
        // Arrange

        var renderer =
            Substitute.For<IEmailTemplateRenderer>();

        var sender =
            Substitute.For<IEmailSender>();

        var model =
            new { FirstName = "Alice" };

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
                    "<p>HTML</p>"));

        EmailMessage? captured =
            null;

        sender
            .SendAsync(
                Arg.Do<EmailMessage>(
                    message => captured = message),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var message =
            new TemplatedEmailMessage
            {
                Subject = "Subject",
                TemplateName = "Template"
            };

        message.To.Add(
            new EmailAddress(
                "to@example.test"));

        var service =
            new TemplatedEmailSender(
                renderer,
                sender);

        // Act

        await service.SendAsync(
            message,
            model,
            culture,
            TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(
            captured);

        Assert.Equal(
            "Subject",
            captured.Subject);

        Assert.Equal(
            "Text",
            captured.TextBody);

        Assert.Equal(
            "<p>HTML</p>",
            captured.HtmlBody);

        Assert.Single(
            captured.To,
            x => x.Address == "to@example.test");

        await renderer
            .Received(1)
            .RenderAsync(
                "Template",
                model,
                culture,
                TestContext.Current.CancellationToken);

        await sender
            .Received(1)
            .SendAsync(
                Arg.Any<EmailMessage>(),
                TestContext.Current.CancellationToken);
    }


    [Fact]
    public async Task TemplatedEmailSender_WhenTemplateReferencesBanner_AddsEmbeddedBannerAsInlineImage()
    {
        // Arrange
        var renderer =
            Substitute.For<IEmailTemplateRenderer>();

        var sender =
            Substitute.For<IEmailSender>();

        var culture =
            CultureInfo.GetCultureInfo("fr-FR");

        var model =
            new { Value = "Value" };

        renderer
            .RenderAsync(
                "Template",
                model,
                culture,
                TestContext.Current.CancellationToken)
            .Returns(
                new RenderedEmailTemplate(
                    "Text",
                    "<img src=\"cid:budget-manager-banner\" alt=\"Budget Manager\" />"));

        string? contentId = null;
        string? contentType = null;
        var streamWasReadable = false;

        sender
            .SendAsync(
                Arg.Do<EmailMessage>(
                    email =>
                    {
                        var image = Assert.Single(email.InlineImages);
                        contentId = image.ContentId;
                        contentType = image.ContentType;
                        streamWasReadable = image.Content.CanRead;
                    }),
                TestContext.Current.CancellationToken)
            .Returns(Task.CompletedTask);

        var message =
            new TemplatedEmailMessage
            {
                Subject = "Subject",
                TemplateName = "Template"
            };

        var service =
            new TemplatedEmailSender(
                renderer,
                sender);

        // Act
        await service.SendAsync(
            message,
            model,
            culture,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            "budget-manager-banner",
            contentId);

        Assert.Equal(
            "image/png",
            contentType);

        Assert.True(streamWasReadable);
    }

    [Fact]
    public async Task TemplatedEmailSender_WhenRendererFails_DoesNotSendMessage()
    {
        // Arrange
        var renderer =
            Substitute.For<IEmailTemplateRenderer>();

        var sender =
            Substitute.For<IEmailSender>();

        var culture =
            CultureInfo.GetCultureInfo("fr-FR");

        var model =
            new { Value = "Value" };

        renderer
            .RenderAsync(
                "Template",
                model,
                culture,
                TestContext.Current.CancellationToken)
            .Returns(
                Task.FromException<RenderedEmailTemplate>(
                    new InvalidOperationException("Rendering failed.")));

        var message =
            new TemplatedEmailMessage
            {
                Subject = "Subject",
                TemplateName = "Template"
            };

        var service =
            new TemplatedEmailSender(
                renderer,
                sender);

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendAsync(
                message,
                model,
                culture,
                TestContext.Current.CancellationToken));

        await sender
            .DidNotReceive()
            .SendAsync(
                Arg.Any<EmailMessage>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SmtpEmailSender_WhenMessageIsNull_Throws()
    {
        var sender =
            CreateSmtpEmailSender();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sender.SendAsync(
                null!,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SmtpEmailSender_WhenMessageHasNoRecipient_ThrowsBeforeConnecting()
    {
        var sender =
            CreateSmtpEmailSender();

        var message =
            new EmailMessage
            {
                Subject = "Subject",
                TextBody = "Body"
            };

        await Assert.ThrowsAsync<ArgumentException>(
            () => sender.SendAsync(
                message,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SmtpEmailSender_WhenSubjectIsEmpty_ThrowsBeforeConnecting()
    {
        var sender =
            CreateSmtpEmailSender();

        var message =
            CreateValidMessage();

        message =
            new EmailMessage
            {
                Subject = " ",
                TextBody = message.TextBody,
                To = [.. message.To]
            };

        await Assert.ThrowsAsync<ArgumentException>(
            () => sender.SendAsync(
                message,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SmtpEmailSender_WhenBodyIsEmpty_ThrowsBeforeConnecting()
    {
        var sender =
            CreateSmtpEmailSender();

        var message =
            new EmailMessage
            {
                Subject = "Subject"
            };

        message.To.Add(
            new EmailAddress(
                "to@example.test"));

        await Assert.ThrowsAsync<ArgumentException>(
            () => sender.SendAsync(
                message,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SmtpEmailSender_WhenSmtpServerAcceptsMessage_SendsMessageAndKeepsAttachmentStreamOpen()
    {
        // Arrange

        await using var server =
            new TestSmtpServer();

        await server.StartAsync(
            TestContext.Current.CancellationToken);

        var sender =
            CreateSmtpEmailSender(
                server.Port,
                SecureSocketOptions.None);

        using var attachmentStream =
            new MemoryStream(
                Encoding.UTF8.GetBytes(
                    "Attachment content"));

        var message =
            new EmailMessage
            {
                Subject = "SMTP integration test",
                TextBody = "Plain body",
                HtmlBody = "<p>HTML body</p>"
            };

        message.To.Add(
            new EmailAddress(
                "to@example.test"));

        message.Attachments.Add(
            new EmailAttachment(
                "attachment.txt",
                attachmentStream,
                "text/plain"));

        // Act

        await sender.SendAsync(
            message,
            TestContext.Current.CancellationToken);

        var receivedMessage =
            await server.WaitForMessageAsync(
                TestContext.Current.CancellationToken);

        // Assert

        Assert.Contains(
            "Subject: SMTP integration test",
            receivedMessage);

        Assert.Contains(
            "to@example.test",
            receivedMessage);

        Assert.True(
            attachmentStream.CanRead);
    }

    [Fact]
    public async Task SmtpEmailSender_WhenSmtpServerRejectsRecipient_Throws()
    {
        // Arrange

        await using var server =
            new TestSmtpServer(
                rejectRecipient: true);

        await server.StartAsync(
            TestContext.Current.CancellationToken);

        var sender =
            CreateSmtpEmailSender(
                server.Port,
                SecureSocketOptions.None);

        var message =
            CreateValidMessage();

        // Act / Assert

        await Assert.ThrowsAsync<MailKit.Net.Smtp.SmtpCommandException>(
            () => sender.SendAsync(
                message,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public void SmtpEmailSender_CreateMessage_MapsAddressesBodiesAttachmentsAndInlineImages()
    {
        // Arrange

        var sender =
            CreateSmtpEmailSender();

        using var attachmentStream =
            new MemoryStream([1, 2, 3]);

        using var inlineImageStream =
            new MemoryStream([4, 5, 6]);

        var message =
            new EmailMessage
            {
                Subject = "Subject",
                TextBody = "Plain text",
                HtmlBody = "<p>HTML</p>"
            };

        message.To.Add(
            new EmailAddress(
                "to@example.test",
                "Recipient"));

        message.Cc.Add(
            new EmailAddress(
                "cc@example.test"));

        message.Bcc.Add(
            new EmailAddress(
                "bcc@example.test"));

        message.Attachments.Add(
            new EmailAttachment(
                "test.txt",
                attachmentStream,
                "text/plain"));

        message.InlineImages.Add(
            new EmailInlineImage(
                "logo",
                inlineImageStream,
                "image/png"));

        // Act

        using var mimeMessage =
            InvokeCreateMessage(
                sender,
                message);

        // Assert

        Assert.Equal(
            "Subject",
            mimeMessage.Subject);

        Assert.Equal(
            "Plain text",
            mimeMessage.TextBody);

        Assert.Equal(
            "<p>HTML</p>",
            mimeMessage.HtmlBody);

        Assert.Single(
            mimeMessage.From.Mailboxes,
            x => x.Address == "noreply@example.test" && x.Name == "Budget Manager");

        Assert.Single(
            mimeMessage.To.Mailboxes,
            x => x.Address == "to@example.test" && x.Name == "Recipient");

        Assert.Single(
            mimeMessage.Cc.Mailboxes,
            x => x.Address == "cc@example.test");

        Assert.Single(
            mimeMessage.Bcc.Mailboxes,
            x => x.Address == "bcc@example.test");

        Assert.Single(
            mimeMessage.Attachments.OfType<MimePart>(),
            x => x.FileName == "test.txt");

        Assert.Single(
            mimeMessage.BodyParts.OfType<MimePart>(),
            x => x.ContentId == "logo");
    }

    private static SmtpEmailSender CreateSmtpEmailSender(
        int port = 587,
        SecureSocketOptions secureOptions = SecureSocketOptions.StartTls)
    {
        var options =
            Options.Create(
                new SmtpOptions
                {
                    Host = port == 587
                        ? "smtp.example.test"
                        : IPAddress.Loopback.ToString(),
                    Port = port,
                    SecureOptions = secureOptions,
                    Timeout = 1000,
                    UserName = string.Empty,
                    Password = string.Empty,
                    FromAddress = "noreply@example.test",
                    FromName = "Budget Manager"
                });

        return new SmtpEmailSender(
            options,
            NullLogger<SmtpEmailSender>.Instance);
    }

    private static EmailMessage CreateValidMessage()
    {
        var message =
            new EmailMessage
            {
                Subject = "Subject",
                TextBody = "Body"
            };

        message.To.Add(
            new EmailAddress(
                "to@example.test"));

        return message;
    }

    private static MimeMessage InvokeCreateMessage(
        SmtpEmailSender sender,
        EmailMessage message)
    {
        var method =
            typeof(SmtpEmailSender).GetMethod(
                "CreateMessage",
                BindingFlags.Instance |
                BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "CreateMessage method was not found.");

        return (MimeMessage)(method.Invoke(
            sender,
            [message])
            ?? throw new InvalidOperationException(
                "CreateMessage returned null."));
    }

    private sealed class TestSmtpServer(
        bool rejectRecipient = false)
        : IAsyncDisposable
    {
        private readonly TcpListener _listener =
            new(
                IPAddress.Loopback,
                0);

        private readonly TaskCompletionSource<string> _messageReceived =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        private Task? _serverTask;

        public int Port =>
            ((IPEndPoint)_listener.LocalEndpoint).Port;

        public Task StartAsync(
            CancellationToken cancellationToken)
        {
            _listener.Start();

            _serverTask =
                RunAsync(
                    cancellationToken);

            return Task.CompletedTask;
        }

        public Task<string> WaitForMessageAsync(
            CancellationToken cancellationToken)
        {
            return _messageReceived.Task.WaitAsync(
                cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();

            if (_serverTask is not null)
            {
                try
                {
                    await _serverTask;
                }
                catch (OperationCanceledException)
                {
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private async Task RunAsync(
            CancellationToken cancellationToken)
        {
            using var client =
                await _listener.AcceptTcpClientAsync(
                    cancellationToken);

            await using var stream =
                client.GetStream();

            using var reader =
                new StreamReader(
                    stream,
                    Encoding.ASCII,
                    false,
                    1024,
                    leaveOpen: true);

            await using var writer =
                new StreamWriter(
                    stream,
                    Encoding.ASCII,
                    1024,
                    leaveOpen: true)
                {
                    NewLine = "\r\n",
                    AutoFlush = true
                };

            await writer.WriteLineAsync(
                "220 localhost BudgetManager test SMTP");

            var data =
                new StringBuilder();

            var readingData =
                false;

            while (!cancellationToken.IsCancellationRequested)
            {
                var line =
                    await reader.ReadLineAsync(
                        cancellationToken);

                if (line is null)
                {
                    return;
                }

                if (readingData)
                {
                    if (line == ".")
                    {
                        readingData = false;

                        _messageReceived.TrySetResult(
                            data.ToString());

                        await writer.WriteLineAsync(
                            "250 2.0.0 Message accepted");
                    }
                    else
                    {
                        data.AppendLine(
                            line);
                    }

                    continue;
                }

                if (line.StartsWith(
                        "EHLO ",
                        StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(
                        "HELO ",
                        StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync(
                        "250-localhost");

                    await writer.WriteLineAsync(
                        "250 8BITMIME");
                }
                else if (line.StartsWith(
                             "MAIL FROM:",
                             StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync(
                        "250 2.1.0 Sender accepted");
                }
                else if (line.StartsWith(
                             "RCPT TO:",
                             StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync(
                        rejectRecipient
                            ? "550 5.1.1 Recipient rejected"
                            : "250 2.1.5 Recipient accepted");
                }
                else if (line.Equals(
                             "DATA",
                             StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync(
                        "354 End data with <CR><LF>.<CR><LF>");

                    readingData =
                        true;
                }
                else if (line.Equals(
                             "QUIT",
                             StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync(
                        "221 2.0.0 Bye");

                    return;
                }
                else
                {
                    await writer.WriteLineAsync(
                        "250 OK");
                }
            }
        }
    }
}
