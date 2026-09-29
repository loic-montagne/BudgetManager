namespace BudgetManager.Web.Models.Budget;

public sealed class BudgetCategoryFormModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAssociated {  get; set; }
}
