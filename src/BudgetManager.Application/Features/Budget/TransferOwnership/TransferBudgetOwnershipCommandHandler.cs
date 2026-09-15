using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.TransferOwnership;

public sealed class TransferBudgetOwnershipCommandHandler(IBudgetRepository budgetRepository, IBudgetContext budgetContext, ICurrentUser currentUser) : IRequestHandler<TransferBudgetOwnershipCommand>
{
    public async Task Handle(TransferBudgetOwnershipCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetContext.GetRequiredAsync(request.BudgetId, cancellationToken);
        var previousOwnerId = currentUser.RequiredUserId;
        budget.TransferOwnership(request.UserId, currentUser.RequiredUserId);
        await budgetRepository.TransferOwnershipAsync(budget, previousOwnerId, cancellationToken);
    }
}
