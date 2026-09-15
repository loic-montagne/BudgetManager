using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.BudgetCategory.Update;

public sealed class UpdateBudgetCategoryCommandHandler(IBudgetCategoryContext budgetCategoryContext, IBudgetCategoryRepository budgetCategoryRepository) : IRequestHandler<UpdateBudgetCategoryCommand>
{
    public async Task Handle(UpdateBudgetCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await budgetCategoryContext.GetRequiredAsync(request.Id, cancellationToken);
        category!.Rename(request.Name);
        category.ChangeDescription(request.Description);
        await budgetCategoryRepository.UpdateAsync(category, cancellationToken);
    }
}
