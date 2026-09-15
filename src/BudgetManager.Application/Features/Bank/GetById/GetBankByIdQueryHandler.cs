using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;
using MediatR;

namespace BudgetManager.Application.Features.Bank.GetById;

public sealed class GetBankByIdQueryHandler(IBankQueries queries) : IRequestHandler<GetBankByIdQuery, BankDto>
{
    public async Task<BankDto> Handle(GetBankByIdQuery request, CancellationToken cancellationToken)
    {
        var bank = await queries.GetByIdAsync(request.Id, cancellationToken);
        NotFoundException<BankDto>.ThrowIfNull(bank, request.Id);
        return bank!;
    }
}
