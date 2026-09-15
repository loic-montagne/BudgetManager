using BudgetManager.Application.Common.Pagination;
using FluentValidation;

namespace BudgetManager.Application.Features.User.Search;

public sealed class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        RuleFor(x => x)
            .Custom((query, context) => PaginationValidatorAdapter.ValidatePagination(query.Criteria.Offset, query.Criteria.Limit, context))
            .Custom((query, context) => PaginationValidatorAdapter.ValidateSorts(query.Criteria.Sorts, context));
    }
}
