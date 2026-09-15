using BudgetManager.Domain.Common;
using BudgetManager.Domain.Entities.Common;
using BudgetManager.Domain.Entities.Interfaces;

namespace BudgetManager.Domain.Entities;

public sealed class BudgetCategory : Entity, IHasOptimisticConcurrencyToken
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; } = null!;

    private readonly List<Budget> _budgets = [];
    public IReadOnlyCollection<Budget> Budgets => _budgets.AsReadOnly();

    /// <summary>
    /// Gets the optimistic-concurrency token maintained by the persistence layer.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private BudgetCategory()
    {
    }

    public static BudgetCategory Create(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > StringPropertyLengths.NameLength)
            throw new ArgumentException($"Name cannot be longer than {StringPropertyLengths.NameLength} characters.", nameof(name));
        if ((NormalizeOptionalText(description) ?? string.Empty).Length > StringPropertyLengths.DescriptionLength)
            throw new ArgumentException($"Description cannot be longer than {StringPropertyLengths.DescriptionLength} characters.", nameof(description));
        return new BudgetCategory()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = NormalizeOptionalText(description)
        };
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > StringPropertyLengths.NameLength)
            throw new ArgumentException($"Name cannot be longer than {StringPropertyLengths.NameLength} characters.", nameof(name));
        Name = name.Trim();
    }
    public void ChangeDescription(string? description)
    {
        if ((NormalizeOptionalText(description) ?? string.Empty).Length > StringPropertyLengths.DescriptionLength)
            throw new ArgumentException($"Description cannot be longer than {StringPropertyLengths.DescriptionLength} characters.", nameof(description));
        Description = NormalizeOptionalText(description);
    }
}
