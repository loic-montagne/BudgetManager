using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.Update;

public sealed class UpdateBudgetCommandHandler(IBudgetRepository budgetRepository, IBudgetContext budgetContext, ICurrentUser currentUser) : IRequestHandler<UpdateBudgetCommand>
{
    public async Task Handle(UpdateBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetContext.GetRequiredAsync(request.Id, cancellationToken);
        budget.Rename(request.Name, currentUser.RequiredUserId);
        await budgetRepository.UpdateAsync(budget, cancellationToken);
    }
}
