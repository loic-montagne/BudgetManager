namespace BudgetManager.Domain.Entities;

public sealed class BudgetCategoryAssociation
{
    public Guid BudgetId { get; private set; }
    public Guid CategoryId { get; private set; }

    public int Order { get; internal set; }

    public Budget Budget { get; private set; } = null!;
    public BudgetCategory Category { get; private set; } = null!;

    private BudgetCategoryAssociation()
    {
    }

    internal static BudgetCategoryAssociation Create(Guid budgetId, Guid categoryId, int order)
    {
        if (budgetId == Guid.Empty)
            throw new ArgumentException("BudgetId cannot be empty.", nameof(budgetId));
        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));

        return new BudgetCategoryAssociation()
        {
            BudgetId = budgetId,
            CategoryId = categoryId,
            Order = order
        };
    }
}
