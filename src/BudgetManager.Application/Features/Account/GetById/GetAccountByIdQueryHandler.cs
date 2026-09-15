using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;
using MediatR;

namespace BudgetManager.Application.Features.Account.GetById;

public sealed class GetAccountByIdQueryHandler(IAccountQueries queries) : IRequestHandler<GetAccountByIdQuery, AccountDto>
{
    public async Task<AccountDto> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await queries.GetByIdAsync(request.Id, cancellationToken);
        NotFoundException<AccountDto>.ThrowIfNull(account, request.Id);
        return account!;
    }
}

