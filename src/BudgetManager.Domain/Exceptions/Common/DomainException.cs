namespace BudgetManager.Domain.Exceptions.Common;

public abstract class DomainException : Exception
{
    public DomainException() : base()
    {
    }
    public DomainException(string? message) : base(message)
    {
    }
}
