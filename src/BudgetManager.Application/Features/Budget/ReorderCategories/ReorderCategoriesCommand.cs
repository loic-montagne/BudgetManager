using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;

namespace BudgetManager.Application.Features.Budget.ReorderCategories;

public sealed record ReorderCategoriesCommand(Guid BudgetId, IReadOnlyCollection<Guid> CategoriesIds) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}
