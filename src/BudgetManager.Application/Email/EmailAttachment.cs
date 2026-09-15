namespace BudgetManager.Application.Email;

/// <summary>
/// Represents a file attached to an email message.
/// </summary>
/// <param name="FileName">
/// The name of the attached file.
/// </param>
/// <param name="Content">
/// The stream containing the attachment content.
/// The caller retains ownership of the stream and is responsible for disposing it.
/// The stream must remain open until the email has been sent.
/// </param>
/// <param name="ContentType">
/// The MIME content type of the attachment.
/// </param>
public sealed record EmailAttachment(
    string FileName,
    Stream Content,
    string ContentType);
