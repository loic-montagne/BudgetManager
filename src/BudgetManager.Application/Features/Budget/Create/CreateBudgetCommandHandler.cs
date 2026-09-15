using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Persistence;
using MediatR;

namespace BudgetManager.Application.Features.Budget.Create;

public sealed class CreateBudgetCommandHandler(IBudgetRepository budgetRepository, ICurrentUser currentUser) : IRequestHandler<CreateBudgetCommand, Guid>
{
    public async Task<Guid> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget = Domain.Entities.Budget.Create(request.Name, currentUser.RequiredUserId);
        await budgetRepository.CreateAsync(budget, cancellationToken);
        return budget.Id;
    }
}
