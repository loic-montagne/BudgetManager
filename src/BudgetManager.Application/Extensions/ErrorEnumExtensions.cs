using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Enums;
using BudgetManager.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace BudgetManager.Application.Extensions;

public static class ErrorEnumExtensions
{
    private static void GetValidationError<T>(this (string message, string code) error, ValidationContext<T> context)
    {
        if (!string.IsNullOrEmpty(error.code) || !string.IsNullOrEmpty(error.message))
        {
            context.AddFailure(new ValidationFailure(context.PropertyPath, error.message)
            {
                ErrorCode = error.code
            });
        }
    }

    public static (string message, string code) GetBudgetEditableStatusError(this BudgetEditableStatus status, bool currentUser = true)
    {
        switch (status)
        {
            case BudgetEditableStatus.NotAuthorized:
                return ($"{(currentUser ? "Current user" : "User")} does not have permission '{Permission.Edit}' on budget.",
                        ErrorCodes.BudgetPermissionInvalid);
            case BudgetEditableStatus.Locked:
                return ("Budget is locked.",
                        ErrorCodes.BudgetIsLocked);
        }
        return (string.Empty, string.Empty);
    }
    public static void GetBudgetEditableStatusError<T>(this BudgetEditableStatus status, ValidationContext<T> context, bool currentUser = true)
    {
        status.GetBudgetEditableStatusError(currentUser)
              .GetValidationError(context);
    }

    public static (string message, string code) GetBudgetLockableStatusError(this BudgetLockableStatus status, bool currentUser = true)
    {
        switch (status)
        {
            case BudgetLockableStatus.NotAuthorized:
                return ($"{(currentUser ? "Current user" : "User")} does not have permission '{Permission.Lock}' on budget.",
                        ErrorCodes.BudgetPermissionInvalid);
            case BudgetLockableStatus.AlreadyLocked:
                return ($"Budget is already locked.",
                        ErrorCodes.BudgetIsLocked);
        }
        return (string.Empty, string.Empty);
    }
    public static void GetBudgetLockableStatusError<T>(this BudgetLockableStatus status, ValidationContext<T> context, bool currentUser = true)
    {
        status.GetBudgetLockableStatusError(currentUser)
              .GetValidationError(context);
    }

    public static (string message, string code) GetBudgetUnlockableStatusError(this BudgetUnlockableStatus status, bool currentUser = true)
    {
        switch (status)
        {
            case BudgetUnlockableStatus.NotAuthorized:
                return ($"{(currentUser ? "Current user" : "User")} does not have permission '{Permission.Lock}' on budget.",
                        ErrorCodes.BudgetPermissionInvalid);
            case BudgetUnlockableStatus.AlreadyUnlocked:
                return ($"Budget is already unlocked.",
                        ErrorCodes.BudgetIsUnlocked);
        }
        return (string.Empty, string.Empty);
    }
    public static void GetBudgetUnlockableStatusError<T>(this BudgetUnlockableStatus status, ValidationContext<T> context, bool currentUser = true)
    {
        status.GetBudgetUnlockableStatusError(currentUser)
              .GetValidationError(context);
    }
}
