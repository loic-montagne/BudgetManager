using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Domain.ValueObjects;
using MediatR;

namespace BudgetManager.Application.Features.Transaction.Update;

public sealed class UpdateTransactionCommandHandler(IBudgetRepository budgetRepository, IBudgetContext budgetContext, ICurrentUser currentUser) : IRequestHandler<UpdateTransactionCommand>
{
    public async Task Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetContext.GetRequiredAsync(request.BudgetId, cancellationToken);
        var amount = new UDecimal(request.Amount);

        budget.RenameTransaction(request.TransactionId, request.Name, currentUser.RequiredUserId);
        budget.ChangeTransactionAmount(request.TransactionId, amount, currentUser.RequiredUserId);
        budget.ChangeTransactionMethod(request.TransactionId, request.Method, request.TransferAccountId, currentUser.RequiredUserId);

        await budgetRepository.UpdateAsync(budget, cancellationToken);
    }
}
