using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Account.GetAll;

public sealed class GetAllAccountsQueryHandler(IAccountQueries queries) : IRequestHandler<GetAllAccountsQuery, IReadOnlyCollection<AccountDto>>
{
    public async Task<IReadOnlyCollection<AccountDto>> Handle(GetAllAccountsQuery request, CancellationToken cancellationToken)
    {
        return await queries.GetAllAsync(cancellationToken);
    }
}
