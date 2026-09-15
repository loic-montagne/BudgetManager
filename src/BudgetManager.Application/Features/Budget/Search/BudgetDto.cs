namespace BudgetManager.Application.Features.Budget.Search;

public sealed record BudgetDto(Guid Id, string Name, bool IsLocked);
