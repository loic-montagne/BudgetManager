namespace BudgetManager.Application.Features.BudgetCategory.Search;

public sealed record BudgetCategoryDto(Guid Id, string Name, string? Description, int BudgetsCount);
