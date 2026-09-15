using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using FluentValidation.Results;

namespace BudgetManager.Application.Exceptions;

public sealed class BadRequestException : Common.ApplicationException
{
    public IReadOnlyCollection<ValidationError> ValidationErrors { get; }

    public BadRequestException(string message) : base(message)
    {
        ValidationErrors = [];
    }
    public BadRequestException(string message, IReadOnlyCollection<ValidationError> errors) : this(message)
    {
        ValidationErrors = errors;
    }
    public BadRequestException(string message, Exception? innerException) : base(message, innerException)
    {
        ValidationErrors = [];
    }
    public BadRequestException(string message, IReadOnlyCollection<ValidationError> errors, Exception? innerException) : this(message, innerException)
    {
        ValidationErrors = errors;
    }

    internal static void ThrowIfResultIsNotValid<T>(Result<T> result, string propertyName)
        where T : class
    {
        if (result.IsValid)
            return;

        var errors = result
            .Errors
            .Select(error => new ValidationError(
                propertyName,
                error.Message,
                error.Code))
            .ToArray();

        if (errors.Length > 0)
            throw new BadRequestException("Request is invalid.", errors);
    }
    internal static void ThrowIfResultIsNotValid(IEnumerable<ValidationFailure>? validationFailures)
    {
        if (validationFailures is null)
            return;

        var errors = validationFailures
            .Select(failure => new ValidationError(
                failure.PropertyName,
                failure.ErrorMessage,
                failure.ErrorCode))
            .ToArray();

        if (errors.Length > 0)
            throw new BadRequestException("Request is invalid.", errors);
    }
}
