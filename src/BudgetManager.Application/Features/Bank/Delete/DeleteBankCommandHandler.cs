using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Bank.Delete;

public sealed class DeleteBankCommandHandler(IBankContext bankContext, IBankRepository bankRepository) : IRequestHandler<DeleteBankCommand>
{
    public async Task Handle(DeleteBankCommand request, CancellationToken cancellationToken)
    {
        var bank = await bankContext.GetRequiredAsync(request.Id, cancellationToken);
        await bankRepository.DeleteAsync(bank, cancellationToken);
    }
}
