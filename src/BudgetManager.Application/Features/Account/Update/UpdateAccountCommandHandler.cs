using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Account.Update;

public sealed class UpdateAccountCommandHandler(IAccountContext accountContext, IAccountRepository accountRepository) : IRequestHandler<UpdateAccountCommand>
{
    public async Task Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accountContext.GetRequiredAsync(request.Id, cancellationToken);
        account.Rename(request.Name);
        await accountRepository.UpdateAsync(account, cancellationToken);
    }
}
