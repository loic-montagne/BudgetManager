namespace BudgetManager.Web.Models.Budget;

public sealed class BudgetCategoriesFormModel
{
    public Guid Id { get; set; }
    public IReadOnlyCollection<BudgetCategoryFormModel> Categories { get; set; } = [];
    public List<Guid> CategoriesIds { get; set; } = [];
}
