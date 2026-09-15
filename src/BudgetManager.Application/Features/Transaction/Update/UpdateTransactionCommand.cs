using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Messaging;
using BudgetManager.Application.Common;
using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Features.Transaction.Update;

public sealed record UpdateTransactionCommand(Guid BudgetId, Guid TransactionId, string Name, decimal Amount, PaymentMethod Method, Guid? TransferAccountId) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredRoles => ApplicationRoles.All;
}
