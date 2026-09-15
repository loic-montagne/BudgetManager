using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.GetAll;

public sealed class GetAllBudgetsQueryHandler(IBudgetQueries queries, ICurrentUser currentUser) : IRequestHandler<GetAllBudgetsQuery, IReadOnlyCollection<BudgetDto>>
{
    public async Task<IReadOnlyCollection<BudgetDto>> Handle(GetAllBudgetsQuery request, CancellationToken cancellationToken)
    {
        return await queries.GetAllAsync(currentUser.RequiredUserId, Domain.Enums.Permission.View, cancellationToken);
    }
}
