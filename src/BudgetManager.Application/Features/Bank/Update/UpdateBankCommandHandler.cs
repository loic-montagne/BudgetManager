using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Bank.Update;

public sealed class UpdateBankCommandHandler(IBankContext bankContext, IBankRepository bankRepository) : IRequestHandler<UpdateBankCommand>
{
    public async Task Handle(UpdateBankCommand request, CancellationToken cancellationToken)
    {
        var bank = await bankContext.GetRequiredAsync(request.Id, cancellationToken);

        bank.Rename(request.Name);
        var bic = Domain.ValueObjects.Bic.Create(request.Bic);
        bank.ChangeBic(bic);

        await bankRepository.UpdateAsync(bank, cancellationToken);
    }
}
