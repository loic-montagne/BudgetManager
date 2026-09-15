using BudgetManager.Domain.Entities.Interfaces;

namespace BudgetManager.Domain.Entities.Common;

public abstract class Entity : Auditable, IEntity
{
    public Guid Id { get; protected set; }

    protected static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }
}
