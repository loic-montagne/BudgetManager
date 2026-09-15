namespace BudgetManager.Application.Exceptions;

public sealed class ConcurrencyException(string? message, Exception innerException) : Common.ApplicationException(message, innerException)
{
}
