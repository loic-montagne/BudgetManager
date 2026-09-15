using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Account.Delete;

public sealed class DeleteAccountCommandHandler(IAccountContext accountContext, IAccountRepository accountRepository) : IRequestHandler<DeleteAccountCommand>
{
    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accountContext.GetRequiredAsync(request.Id, cancellationToken);
        await accountRepository.DeleteAsync(account, cancellationToken);
    }
}
