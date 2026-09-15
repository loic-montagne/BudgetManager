using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Pagination;
using BudgetManager.Application.Enums;

namespace BudgetManager.Application.Features.BudgetCategory.Search;

public sealed record SearchBudgetCategoriesQuery(PagedSearchCriteria<BudgetCategorySortField> Criteria) : IQuery<PagedResult<BudgetCategoryDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}
