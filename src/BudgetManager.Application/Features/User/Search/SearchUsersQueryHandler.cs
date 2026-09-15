using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Pagination;
using MediatR;

namespace BudgetManager.Application.Features.User.Search;

public sealed class SearchUsersQueryHandler(IUserQueries queries) : IRequestHandler<SearchUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        return await queries.SearchAsync(request.Criteria, cancellationToken);
    }
}
