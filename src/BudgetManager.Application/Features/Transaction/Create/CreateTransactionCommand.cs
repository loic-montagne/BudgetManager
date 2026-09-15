using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;
using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Features.Transaction.Create;

public sealed record CreateTransactionCommand(Guid BudgetId, Guid CategoryId, Guid AccountId, string Name, TransactionType Type, decimal Amount, PaymentMethod Method, Guid? TransferAccountId) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}
