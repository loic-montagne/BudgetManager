using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.BudgetCategory.Create;

public sealed class CreateBudgetCategoryCommandHandler(IBudgetCategoryRepository budgetCategoryRepository) : IRequestHandler<CreateBudgetCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateBudgetCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Domain.Entities.BudgetCategory.Create(request.Name, request.Description);
        await budgetCategoryRepository.CreateAsync(category, cancellationToken);
        return category.Id;
    }
}
