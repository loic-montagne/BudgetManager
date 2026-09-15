using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;

namespace BudgetManager.Application.Features.Account.Create;

public sealed record CreateAccountCommand(string Name, string Iban, Guid BankId) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => [ApplicationRoles.Administrator];
}
