using BudgetManager.Application.Common.Errors;
using BudgetManager.Domain.Exceptions;
using BudgetManager.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;

namespace BudgetManager.Application.Features.Bank.Common;

internal static class BicValidatorAdapter
{
    internal static void ValidateBic<T>(string bic, ValidationContext<T> context)
    {
        if (string.IsNullOrWhiteSpace(bic))
            return;

        try
        {
            Bic.Create(bic);
        }
        catch (InvalidBicException ex)
        {
            context.AddFailure(
                new ValidationFailure(context.PropertyPath, ex.Message)
                { 
                    ErrorCode = string.Format(ErrorCodes.BankErrorCodeFormat, ex.ErrorCode)
                });
        }
    }
}
