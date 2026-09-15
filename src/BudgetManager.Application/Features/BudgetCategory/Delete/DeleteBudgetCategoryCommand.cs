using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;

namespace BudgetManager.Application.Features.BudgetCategory.Delete;

public sealed record DeleteBudgetCategoryCommand(Guid Id) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => [ApplicationRoles.Administrator];
}
