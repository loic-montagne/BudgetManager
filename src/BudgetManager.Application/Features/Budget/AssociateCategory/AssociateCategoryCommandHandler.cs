using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.AssociateCategory;

public sealed class AssociateCategoryCommandHandler(IBudgetRepository budgetRepository, IBudgetContext budgetContext, ICurrentUser currentUser) : IRequestHandler<AssociateCategoryCommand>
{
    public async Task Handle(AssociateCategoryCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetContext.GetRequiredAsync(request.BudgetId, cancellationToken);

        budget.AssociateCategory(request.CategoryId, currentUser.RequiredUserId);

        await budgetRepository.UpdateAsync(budget, cancellationToken);
    }
}
