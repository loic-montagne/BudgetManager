using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;

namespace BudgetManager.Application.Features.Budget.Update;

public sealed record UpdateBudgetCommand(Guid Id, string Name) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}
