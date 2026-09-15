using BudgetManager.Application.Common.Pagination;
using FluentValidation;

namespace BudgetManager.Application.Features.BudgetCategory.Search;

public sealed class SearchBudgetCategoriesQueryValidator : AbstractValidator<SearchBudgetCategoriesQuery>
{
    public SearchBudgetCategoriesQueryValidator()
    {
        RuleFor(x => x)
            .Custom((query, context) => PaginationValidatorAdapter.ValidatePagination(query.Criteria.Offset, query.Criteria.Limit, context))
            .Custom((query, context) => PaginationValidatorAdapter.ValidateSorts(query.Criteria.Sorts, context));
    }
}
