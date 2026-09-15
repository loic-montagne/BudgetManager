using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.BudgetCategory.GetAll;

public sealed class GetAllBudgetCategoriesQueryHandler(IBudgetCategoryQueries queries) : IRequestHandler<GetAllBudgetCategoriesQuery, IReadOnlyCollection<BudgetCategoryDto>>
{
    public async Task<IReadOnlyCollection<BudgetCategoryDto>> Handle(GetAllBudgetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await queries.GetAllAsync(cancellationToken);
    }
}

