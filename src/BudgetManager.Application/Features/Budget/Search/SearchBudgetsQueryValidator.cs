using BudgetManager.Application.Common.Pagination;
using FluentValidation;

namespace BudgetManager.Application.Features.Budget.Search;

public sealed class SearchBudgetsQueryValidator : AbstractValidator<SearchBudgetsQuery>
{
    public SearchBudgetsQueryValidator()
    {
        RuleFor(x => x)
            .Custom((query, context) => PaginationValidatorAdapter.ValidatePagination(query.Criteria.Offset, query.Criteria.Limit, context))
            .Custom((query, context) => PaginationValidatorAdapter.ValidateSorts(query.Criteria.Sorts, context));
    }
}