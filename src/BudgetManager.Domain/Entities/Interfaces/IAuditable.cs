namespace BudgetManager.Domain.Entities.Interfaces;

public interface IAuditable
{
    public Guid CreatedBy { get; }
    public DateTimeOffset CreatedOn { get; }
    public Guid UpdatedBy { get; }
    public DateTimeOffset UpdatedOn { get; }

    public void MarkCreated(DateTimeOffset dateTime, Guid? userId);
    public void MarkUpdated(DateTimeOffset dateTime, Guid? userId);
}
