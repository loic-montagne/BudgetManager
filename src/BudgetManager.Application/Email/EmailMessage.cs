namespace BudgetManager.Application.Email;

public sealed class EmailMessage : EmailMessageBase
{
    public string? TextBody { get; init; }

    public string? HtmlBody { get; init; }

    public bool IsBodyEmpty => string.IsNullOrWhiteSpace(TextBody) && string.IsNullOrWhiteSpace(HtmlBody);
}