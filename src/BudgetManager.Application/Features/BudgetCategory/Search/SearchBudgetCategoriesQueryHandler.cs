using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Pagination;
using MediatR;

namespace BudgetManager.Application.Features.BudgetCategory.Search;

public sealed class SearchBudgetCategoriesQueryHandler(IBudgetCategoryQueries queries) : IRequestHandler<SearchBudgetCategoriesQuery, PagedResult<BudgetCategoryDto>>
{
    public async Task<PagedResult<BudgetCategoryDto>> Handle(SearchBudgetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await queries.SearchAsync(request.Criteria, cancellationToken);
    }
}
