using MailKit.Security;

namespace BudgetManager.Infrastructure.Configuration;

internal sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public required string Host { get; init; }

    public int Port { get; init; }
    
    public SecureSocketOptions SecureOptions { get; init; }

    public int Timeout { get; init; } = 10000;

    public required string UserName { get; init; }

    public required string Password { get; init; }

    public required string FromAddress { get; init; }

    public required string FromName { get; init; }
}
