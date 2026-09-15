using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Account.Create;

public sealed class CreateAccountCommandHandler(IAccountRepository accountRepository) : IRequestHandler<CreateAccountCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var iban = Domain.ValueObjects.Iban.Create(request.Iban);
        var account = Domain.Entities.Account.Create(request.Name, request.BankId, iban);
        await accountRepository.CreateAsync(account, cancellationToken);        
        return account.Id;
    }
}
