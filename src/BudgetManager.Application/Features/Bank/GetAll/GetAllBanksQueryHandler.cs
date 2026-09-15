using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Bank.GetAll;

public sealed class GetAllBanksQueryHandler(IBankQueries queries) : IRequestHandler<GetAllBanksQuery, IReadOnlyCollection<BankDto>>
{
    public async Task<IReadOnlyCollection<BankDto>> Handle(GetAllBanksQuery request, CancellationToken cancellationToken)
    {
        return await queries.GetAllAsync(cancellationToken);
    }
}
