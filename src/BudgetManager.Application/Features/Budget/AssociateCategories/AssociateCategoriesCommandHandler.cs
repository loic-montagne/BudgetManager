using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.AssociateCategories;

public sealed class AssociateCategoriesCommandHandler(IBudgetRepository budgetRepository, IBudgetContext budgetContext, ICurrentUser currentUser) : IRequestHandler<AssociateCategoriesCommand>
{
    public async Task Handle(AssociateCategoriesCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetContext.GetRequiredAsync(request.BudgetId, cancellationToken);
        if (request.CategoriesIds?.Any() == true)
        {
            foreach (var id in request.CategoriesIds)
                budget.AssociateCategory(id, currentUser.RequiredUserId);
            await budgetRepository.UpdateAsync(budget, cancellationToken);
        }
    }
}
