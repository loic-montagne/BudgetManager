using BudgetManager.Application.Common.Errors;
using BudgetManager.Domain.Extensions;
using FluentValidation;
using FluentValidation.Results;

namespace BudgetManager.Application.Common.Pagination;

internal static class PaginationValidatorAdapter
{
    internal static void ValidatePagination<T>(int? offset, int? limit, ValidationContext<T> context)
    {
        if (offset.HasValue != limit.HasValue)
        {
            context.AddFailure(new ValidationFailure(
                "Pagination",
                "Offset and Limit must both be specified or both be null.")
                {
                    ErrorCode = ErrorCodes.SearchPaginationInvalid
                });
        }

        if (offset.HasValue && offset.Value < 0)
        {
            context.AddFailure(
                new ValidationFailure(
                    "Offset",
                    "Offset must be greater than or equal to 0.")
                {
                    ErrorCode = ErrorCodes.SearchOffsetInvalid
                });
        }

        if (limit.HasValue && (limit < 1 || limit > 100))
        {
            context.AddFailure(
                new ValidationFailure(
                    "Limit",
                    "Limit must be between 1 and 100.")
                {
                    ErrorCode = ErrorCodes.SearchLimitInvalid
                });
        }
    }

    internal static void ValidateSorts<TSortFieldEnum, T>(IReadOnlyList<SortCriterion<TSortFieldEnum>>? sorts, ValidationContext<T> context)
         where TSortFieldEnum : struct, Enum
    {
        if (sorts is not null && sorts.Any())
        {
            foreach (var sort in sorts)
            {
                if (!sort.Direction.IsValid())
                {
                    context.AddFailure(new ValidationFailure(
                        "Sort",
                        $"Sort direction {sort.Direction} is not valid.")
                    {
                        ErrorCode = ErrorCodes.SearchSortDirectionInvalid
                    });
                }
                if (!sort.Field.IsValid())
                {
                    context.AddFailure(new ValidationFailure(
                        "Sort",
                        $"Sort field {sort.Field} is not valid.")
                    {
                        ErrorCode = ErrorCodes.SearchSortFieldInvalid
                    });
                }
            }
        }
    }
}
