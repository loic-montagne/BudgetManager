namespace BudgetManager.Application.Features.BudgetCategory.GetById;

public sealed record BudgetCategoryDto(Guid Id, string Name, string? Description, int BudgetsCount, Guid CreatedBy, string CreatedByName, DateTimeOffset CreatedOn, Guid UpdatedBy, string UpdatedByName, DateTimeOffset UpdatedOn);
