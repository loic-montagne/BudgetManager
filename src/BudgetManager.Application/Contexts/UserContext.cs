using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.GetById;

namespace BudgetManager.Application.Contexts;

internal sealed class UserContext(IUserQueries userQueries, EntityCacheContext cache) : IUserContext
{
    public async Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (cache.TryGet<UserDto>(id, out var cachedUser))
            return cachedUser;

        var user = await userQueries.GetByIdAsync(id, cancellationToken);

        if (user is not null)
            cache.Set(id, user);

        return user;
    }

    public async Task<UserDto> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetAsync(id, cancellationToken);
        NotFoundException<UserDto>.ThrowIfNull(user, id);
        return user!;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetAsync(id, cancellationToken);
        return user != null;
    }
}
