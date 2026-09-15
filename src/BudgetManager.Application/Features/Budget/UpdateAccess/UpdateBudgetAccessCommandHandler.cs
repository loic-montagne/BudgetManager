using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.UpdateAccess;

public sealed class UpdateBudgetAccessCommandHandler(IBudgetRepository budgetRepository, IBudgetContext budgetContext, ICurrentUser currentUser) : IRequestHandler<UpdateBudgetAccessCommand>
{
    public async Task Handle(UpdateBudgetAccessCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetContext.GetRequiredAsync(request.BudgetId, cancellationToken);
        budget.SetPermissions(request.UserId, request.Permissions, currentUser.RequiredUserId);
        await budgetRepository.UpdateAsync(budget, cancellationToken);
    }
}
