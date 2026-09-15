namespace BudgetManager.Application.Features.Budget.GetAll;

public sealed record BudgetDto(Guid Id, string Name, bool IsLocked);
