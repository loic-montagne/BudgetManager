namespace BudgetManager.Application.Email;

/// <summary>
/// Represents an image embedded in the HTML body of an email message.
/// </summary>
/// <param name="ContentId">
/// The identifier used to reference the image from the HTML body.
/// </param>
/// <param name="Content">
/// The stream containing the image content.
/// The caller retains ownership of the stream and is responsible for disposing it.
/// The stream must remain open until the email has been sent.
/// </param>
/// <param name="ContentType">
/// The MIME content type of the image.
/// </param>
public sealed record EmailInlineImage(
    string ContentId,
    Stream Content,
    string ContentType);
