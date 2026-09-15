using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Pagination;
using MediatR;

namespace BudgetManager.Application.Features.Budget.Search;

public sealed class SearchBudgetsQueryHandler(IBudgetQueries queries, ICurrentUser currentUser) : IRequestHandler<SearchBudgetsQuery, PagedResult<BudgetDto>>
{
    public async Task<PagedResult<BudgetDto>> Handle(SearchBudgetsQuery request, CancellationToken cancellationToken)
    {
        return await queries.SearchAsync(request.Criteria, currentUser.RequiredUserId, Domain.Enums.Permission.View, cancellationToken);
    }
}
