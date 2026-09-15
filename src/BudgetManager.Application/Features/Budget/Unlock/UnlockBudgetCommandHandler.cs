using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.Unlock;

public sealed class UnlockBudgetCommandHandler(IBudgetRepository budgetRepository, IBudgetContext budgetContext, ICurrentUser currentUser) : IRequestHandler<UnlockBudgetCommand>
{
    public async Task Handle(UnlockBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetContext.GetRequiredAsync(request.Id, cancellationToken);
        budget.Unlock(currentUser.RequiredUserId);
        await budgetRepository.UpdateAsync(budget, cancellationToken);
    }
}
