namespace BudgetManager.Application.Exceptions;

public abstract class NotFoundException : Common.ApplicationException
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public sealed class NotFoundException<TEntity> : NotFoundException
{
    public NotFoundException(object key) : base($"Entity {typeof(TEntity).Name} with key '{key}' was not found.")
    {
    }

    internal static void ThrowIfNull(TEntity? entity, object key)
    {
        if (entity is null)
            throw new NotFoundException<TEntity>(key);
    }
}
