using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Common.Pagination;

namespace BudgetManager.Application.Abstractions.Persistence;

/// <summary>
/// Provides read-only user projections required by application validation.
/// </summary>
public interface IUserQueries
{
    Task<Features.User.GetById.UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Features.User.GetById.CompleteUserDto?> GetCompleteByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Features.User.GetCurrent.CurrentUserDto?> GetCurrentAsync(ICurrentUser currentUser, CancellationToken cancellationToken);
    Task<PagedResult<Features.User.Search.UserDto>> SearchAsync(Features.User.Search.PagedSearchCriteria criteria, CancellationToken cancellationToken);
}
