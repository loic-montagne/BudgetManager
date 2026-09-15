using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;
using MediatR;

namespace BudgetManager.Application.Features.BudgetAccess.GetByKey;

public sealed class GetBudgetAccessByKeyQueryHandler(IBudgetAccessQueries queries, ICurrentUser currentUser) : IRequestHandler<GetBudgetAccessByKeyQuery, BudgetAccessDto>
{
    public async Task<BudgetAccessDto> Handle(GetBudgetAccessByKeyQuery request, CancellationToken cancellationToken)
    {
        var access = await queries.GetByKeyAsync(request.BudgetId, request.UserId, currentUser.RequiredUserId, Domain.Enums.Permission.Share, cancellationToken);
        NotFoundException<BudgetAccessDto>.ThrowIfNull(access, new { request.BudgetId, request.UserId });
        return access!;
    }
}
