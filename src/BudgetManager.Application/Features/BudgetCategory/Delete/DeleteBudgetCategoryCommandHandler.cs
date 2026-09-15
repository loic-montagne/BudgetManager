using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.BudgetCategory.Delete;

public sealed class DeleteBudgetCategoryCommandHandler(IBudgetCategoryContext budgetCategoryContext, IBudgetCategoryRepository budgetCategoryRepository) : IRequestHandler<DeleteBudgetCategoryCommand>
{
    public async Task Handle(DeleteBudgetCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await budgetCategoryContext.GetRequiredAsync(request.Id, cancellationToken);
        await budgetCategoryRepository.DeleteAsync(category, cancellationToken);
    }
}
