using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;
using MediatR;

namespace BudgetManager.Application.Features.Budget.GetById;

public sealed class GetBudgetByIdQueryHandler(IBudgetQueries queries, ICurrentUser currentUser) : IRequestHandler<GetBudgetByIdQuery, BudgetDto>
{
    public async Task<BudgetDto> Handle(GetBudgetByIdQuery request, CancellationToken cancellationToken)
    {
        var budget = await queries.GetByIdAsync(request.Id, currentUser.RequiredUserId, Domain.Enums.Permission.View, cancellationToken);
        NotFoundException<BudgetDto>.ThrowIfNull(budget, request.Id);
        return budget!;
    }
}
