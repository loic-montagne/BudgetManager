using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Pagination;

namespace BudgetManager.Application.Features.User.Search;

public sealed record SearchUsersQuery(PagedSearchCriteria Criteria) : IQuery<PagedResult<UserDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => [ApplicationRoles.Administrator];
}
