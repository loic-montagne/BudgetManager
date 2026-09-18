using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.ReorderCategories;

public sealed class ReorderCategoriesCommandHandler(IBudgetRepository budgetRepository, IBudgetContext budgetContext, ICurrentUser currentUser) : IRequestHandler<ReorderCategoriesCommand>
{
    public async Task Handle(ReorderCategoriesCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetContext.GetRequiredAsync(request.BudgetId, cancellationToken);
        budget.ReorderCategories(request.CategoriesIds, currentUser.RequiredUserId);
        await budgetRepository.UpdateAsync(budget, cancellationToken);
    }
}
