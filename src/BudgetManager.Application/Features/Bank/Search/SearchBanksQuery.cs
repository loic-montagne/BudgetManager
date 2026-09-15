using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;

namespace BudgetManager.Application.Features.Bank.Search;

public sealed record SearchBanksQuery(PagedSearchCriteria<BankSortField> Criteria) : IQuery<PagedResult<BankDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => [ApplicationRoles.Administrator];
}
