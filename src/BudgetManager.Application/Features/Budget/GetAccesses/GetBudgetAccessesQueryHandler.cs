using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.GetAccesses;

public sealed class GetBudgetAccessesQueryHandler(IBudgetAccessQueries queries, ICurrentUser currentUser) : IRequestHandler<GetBudgetAccessesQuery, IReadOnlyCollection<BudgetAccessDto>>
{
    public async Task<IReadOnlyCollection<BudgetAccessDto>> Handle(GetBudgetAccessesQuery request, CancellationToken cancellationToken)
    {
        return await queries.GetByBudgetIdAsync(request.Id, currentUser.RequiredUserId, Domain.Enums.Permission.Share, cancellationToken);
    }
}
