namespace BudgetManager.Application.Email;

public abstract class EmailMessageBase
{
    public required string Subject { get; init; }

    public IList<EmailAddress> To { get; internal init; } = [];

    public IList<EmailAddress> Cc { get; internal init; } = [];

    public IList<EmailAddress> Bcc { get; internal init; } = [];

    public IList<EmailAttachment> Attachments { get; internal init; } = [];

    public IList<EmailInlineImage> InlineImages { get; internal init; } = [];
}
