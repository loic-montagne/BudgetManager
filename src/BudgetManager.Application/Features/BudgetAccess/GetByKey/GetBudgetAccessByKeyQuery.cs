using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;

namespace BudgetManager.Application.Features.BudgetAccess.GetByKey;

public sealed record GetBudgetAccessByKeyQuery(Guid BudgetId, Guid UserId) : IQuery<BudgetAccessDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}
