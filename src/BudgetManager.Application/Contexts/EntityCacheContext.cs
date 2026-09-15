namespace BudgetManager.Application.Contexts;

/// <summary>
/// Stores tracked entities for the lifetime of the current dependency-injection scope.
/// </summary>
internal sealed class EntityCacheContext
{
    private readonly Dictionary<(Type Type, Guid Id), object> _entities = [];

    public bool TryGet<TEntity>(Guid id, out TEntity? entity)
        where TEntity : class
    {
        if (_entities.TryGetValue((typeof(TEntity), id), out var value))
        {
            entity = (TEntity)value;
            return true;
        }

        entity = null;
        return false;
    }

    public void Set<TEntity>(Guid id, TEntity entity)
        where TEntity : class
    {
        _entities[(typeof(TEntity), id)] = entity;
    }
}
