using BudgetManager.Domain.Exceptions.Common;

namespace BudgetManager.Domain.Exceptions;

public sealed class InvalidIbanException : DomainException
{
    public string? ErrorCode { get; private set; }
    public InvalidIbanException(string? message, string? errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}
