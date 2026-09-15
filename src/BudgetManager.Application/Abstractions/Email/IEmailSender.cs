using BudgetManager.Application.Email;

namespace BudgetManager.Application.Abstractions.Email;

public interface IEmailSender
{
    /// <summary>
    /// Sends an email message.
    /// </summary>
    /// <remarks>
    /// Streams referenced by attachments and inline images remain owned by
    /// the caller. This method does not dispose them. They must remain open
    /// until the returned task has completed.
    /// </remarks>
    /// <param name="message">
    /// The email message to send.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel the operation.
    /// </param>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
