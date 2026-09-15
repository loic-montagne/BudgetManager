using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;

namespace BudgetManager.Application.Features.BudgetCategory.GetAll;

public sealed record GetAllBudgetCategoriesQuery : IQuery<IReadOnlyCollection<BudgetCategoryDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}
