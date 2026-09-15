using BudgetManager.Application.Features.User.GetById;

namespace BudgetManager.Application.Abstractions.Contexts;

/// <summary>
/// Provides scoped access to user projections used by application validation.
/// </summary>
public interface IUserContext
{
    Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<UserDto> GetRequiredAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
