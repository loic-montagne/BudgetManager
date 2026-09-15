using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;
using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Features.Budget.UpdateAccess;

public sealed record UpdateBudgetAccessCommand(Guid BudgetId, Guid UserId, Permission Permissions) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}
