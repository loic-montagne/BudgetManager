using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;
using MediatR;

namespace BudgetManager.Application.Features.BudgetCategory.GetById;

public sealed class GetBudgetCategoryByIdQueryHandler(IBudgetCategoryQueries queries) : IRequestHandler<GetBudgetCategoryByIdQuery, BudgetCategoryDto>
{
    public async Task<BudgetCategoryDto> Handle(GetBudgetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await queries.GetByIdAsync(request.Id, cancellationToken);
        NotFoundException<BudgetCategoryDto>.ThrowIfNull(category, request.Id);
        return category!;
    }
}
