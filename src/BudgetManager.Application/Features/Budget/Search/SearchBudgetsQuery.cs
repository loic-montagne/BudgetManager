using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Pagination;

namespace BudgetManager.Application.Features.Budget.Search;

public sealed record SearchBudgetsQuery(PagedSearchCriteria Criteria) : IQuery<PagedResult<BudgetDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}
