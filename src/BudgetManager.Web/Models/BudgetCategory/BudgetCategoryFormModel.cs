using BudgetManager.Web.Models.Common;

namespace BudgetManager.Web.Models.BudgetCategory;

public sealed class BudgetCategoryFormModel : AuditableFormModel
{
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int BudgetsCount { get; set; }
}
