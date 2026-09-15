using BudgetManager.Application.Common.Pagination;
using FluentValidation;

namespace BudgetManager.Application.Features.Account.Search;

public sealed class SearchAccountsQueryValidator : AbstractValidator<SearchAccountsQuery>
{
    public SearchAccountsQueryValidator()
    {
        RuleFor(x => x)
            .Custom((query, context) => PaginationValidatorAdapter.ValidatePagination(query.Criteria.Offset, query.Criteria.Limit, context))
            .Custom((query, context) => PaginationValidatorAdapter.ValidateSorts(query.Criteria.Sorts, context));
    }
}
