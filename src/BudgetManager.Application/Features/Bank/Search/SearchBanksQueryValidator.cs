using BudgetManager.Application.Common.Pagination;
using FluentValidation;

namespace BudgetManager.Application.Features.Bank.Search;

public sealed class SearchBanksQueryValidator : AbstractValidator<SearchBanksQuery>
{
    public SearchBanksQueryValidator()
    {
        RuleFor(x => x)
            .Custom((query, context) => PaginationValidatorAdapter.ValidatePagination(query.Criteria.Offset, query.Criteria.Limit, context))
            .Custom((query, context) => PaginationValidatorAdapter.ValidateSorts(query.Criteria.Sorts, context));
    }
}
