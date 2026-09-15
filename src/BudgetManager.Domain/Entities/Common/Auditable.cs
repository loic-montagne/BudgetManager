using BudgetManager.Domain.Entities.Interfaces;

namespace BudgetManager.Domain.Entities.Common;

public abstract class Auditable : IAuditable
{
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset UpdatedOn { get; private set; }

    public void MarkCreated(DateTimeOffset dateTime, Guid? userId)
    {
        if (CreatedOn != default)
            throw new InvalidOperationException("Entity is already marked as created.");

        CreatedOn = dateTime;
        CreatedBy = userId ?? Guid.Empty;
        UpdatedOn = dateTime;
        UpdatedBy = userId ?? Guid.Empty;
    }

    public void MarkUpdated(DateTimeOffset dateTime, Guid? userId)
    {
        if (CreatedOn == default)
            throw new InvalidOperationException("Entity has not been created.");

        ArgumentOutOfRangeException.ThrowIfLessThan(dateTime, UpdatedOn, nameof(dateTime));

        UpdatedOn = dateTime;
        if (userId.HasValue)
            UpdatedBy = userId.Value;
    }
}
