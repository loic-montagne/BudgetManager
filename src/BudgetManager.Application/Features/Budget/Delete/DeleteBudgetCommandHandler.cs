using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.Delete;

public sealed class DeleteBudgetCommandHandler(IBudgetRepository budgetRepository, IBudgetContext budgetContext) : IRequestHandler<DeleteBudgetCommand>
{
    public async Task Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetContext.GetRequiredAsync(request.Id, cancellationToken);
        await budgetRepository.DeleteAsync(budget, cancellationToken);
    }
}
