using BudgetManager.Web.Models.Common;

namespace BudgetManager.Web.Models.Budget;

public sealed class BudgetFormModel : AuditableFormModel
{
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
