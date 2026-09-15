namespace BudgetManager.Application.Features.BudgetCategory.GetAll;

public sealed record BudgetCategoryDto(Guid Id, string Name, string? Description, int BudgetsCount);
