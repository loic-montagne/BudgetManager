using BudgetManager.Application.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace BudgetManager.Application.Behaviors;

/// <summary>
/// Executes FluentValidation validators sequentially and prevents handler execution when validation failures are found.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var failures = new List<ValidationFailure>();
        foreach (var validator in validators)
        {
            var context = new ValidationContext<TRequest>(request);
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors);
        }

        BadRequestException.ThrowIfResultIsNotValid(failures);

        return await next(cancellationToken);
    }
}
