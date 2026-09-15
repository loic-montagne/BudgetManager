using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Bank.Create;

public sealed class CreateBankCommandHandler(IBankRepository bankRepository) : IRequestHandler<CreateBankCommand, Guid>
{
    public async Task<Guid> Handle(CreateBankCommand request, CancellationToken cancellationToken)
    {
        var bic = Domain.ValueObjects.Bic.Create(request.Bic);
        var bank = Domain.Entities.Bank.Create(request.Name, bic);
        await bankRepository.CreateAsync(bank, cancellationToken);        
        return bank.Id;
    }
}
