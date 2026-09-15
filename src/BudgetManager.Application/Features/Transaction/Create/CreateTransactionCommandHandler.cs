using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Domain.ValueObjects;
using MediatR;

namespace BudgetManager.Application.Features.Transaction.Create;

public sealed class CreateTransactionCommandHandler(IBudgetRepository budgetRepository, IBudgetContext budgetContext, ICurrentUser currentUser) : IRequestHandler<CreateTransactionCommand>
{
    public async Task Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetContext.GetRequiredAsync(request.BudgetId, cancellationToken);
        var amount = new UDecimal(request.Amount);

        budget.AddTransaction(request.CategoryId, request.AccountId, request.Name, request.Type, amount, request.Method, request.TransferAccountId, currentUser.RequiredUserId);

        await budgetRepository.UpdateAsync(budget, cancellationToken);
    }
}
