using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.AssociateCategory;

public sealed class AssociateCategoryCommandHandler(IBudgetRepository budgetRepository, IBudgetContext budgetContext, IBudgetCategoryContext budgetCategoryContext, ICurrentUser currentUser) : IRequestHandler<AssociateCategoryCommand>
{
    public async Task Handle(AssociateCategoryCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetContext.GetRequiredAsync(request.BudgetId, cancellationToken);
        var category = await budgetCategoryContext.GetRequiredAsync(request.CategoryId, cancellationToken);

        budget.AssociateCategory(category, currentUser.RequiredUserId);

        await budgetRepository.UpdateAsync(budget, cancellationToken);
    }
}
