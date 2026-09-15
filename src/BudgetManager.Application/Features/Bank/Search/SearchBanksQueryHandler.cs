using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Pagination;
using MediatR;

namespace BudgetManager.Application.Features.Bank.Search;

public sealed class SearchBanksQueryHandler(IBankQueries queries) : IRequestHandler<SearchBanksQuery, PagedResult<BankDto>>
{
    public async Task<PagedResult<BankDto>> Handle(SearchBanksQuery request, CancellationToken cancellationToken)
    {
        return await queries.SearchAsync(request.Criteria, cancellationToken);
    }
}
