namespace BudgetManager.Application.Exceptions;

public sealed class UpdateException(string? message, Exception innerException) : ApplicationException(message, innerException)
{
}
