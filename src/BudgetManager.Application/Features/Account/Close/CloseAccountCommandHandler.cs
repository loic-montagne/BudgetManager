using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Account.Close;

public sealed class CloseAccountCommandHandler(IAccountContext accountContext, IAccountRepository accountRepository) : IRequestHandler<CloseAccountCommand>
{
    public async Task Handle(CloseAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accountContext.GetRequiredAsync(request.Id, cancellationToken);
        account.Close();
        await accountRepository.UpdateAsync(account, cancellationToken);
    }
}
