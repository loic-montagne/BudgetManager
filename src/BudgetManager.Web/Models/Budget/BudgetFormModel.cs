namespace BudgetManager.Web.Models.Budget;

public sealed class BudgetFormModel
{
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
