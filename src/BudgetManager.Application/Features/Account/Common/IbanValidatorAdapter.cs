using BudgetManager.Application.Common.Errors;
using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;

namespace BudgetManager.Application.Features.Account.Common;

internal static class IbanValidatorAdapter
{
    internal static void ValidateIban<T>(string iban, ValidationContext<T> context)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return;

        try
        {
            Iban.Create(iban);
        }
        catch (InvalidIbanException ex)
        {
            context.AddFailure(
                new ValidationFailure(context.PropertyPath, ex.Message)
                {
                    ErrorCode = string.Format(ErrorCodes.AccountErrorCodeFormat, ex.ErrorCode)
                });
        }
    }
}
