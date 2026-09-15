using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Pagination;
using MediatR;

namespace BudgetManager.Application.Features.Account.Search;

public sealed class SearchAccountsQueryHandler(IAccountQueries queries) : IRequestHandler<SearchAccountsQuery, PagedResult<AccountDto>>
{
    public async Task<PagedResult<AccountDto>> Handle(SearchAccountsQuery request, CancellationToken cancellationToken)
    {
        return await queries.SearchAsync(request.Criteria, cancellationToken);
    }
}
