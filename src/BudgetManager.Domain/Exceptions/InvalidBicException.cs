using BudgetManager.Domain.Exceptions.Common;

namespace BudgetManager.Domain.Exceptions;

public sealed class InvalidBicException : DomainException
{
    public string? ErrorCode { get; private set; }
    public InvalidBicException(string? message, string? errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}
