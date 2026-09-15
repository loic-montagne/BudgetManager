using BudgetManager.Application.Exceptions;

namespace BudgetManager.Application.Abstractions.Authentication;

/// <summary>
/// Provides the identity, roles and claims of the user associated with the current execution context.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    
    Guid? UserId { get; }
    string? UserName { get; }

    /// <summary>
    /// Gets the authenticated user identifier.
    /// </summary>
    /// <exception cref="UnauthenticatedException">The current user is not authenticated.</exception>
    Guid RequiredUserId => UserId ?? throw new UnauthenticatedException(this);
    /// <summary>
    /// Gets the authenticated user name.
    /// </summary>
    /// <exception cref="UnauthenticatedException">The current user is not authenticated.</exception>
    string RequiredUserName => UserName ?? throw new UnauthenticatedException(this);

    IEnumerable<string> RolesNames { get; }

    bool IsInRole(string role);
    string? GetClaim(string type);
    bool HasClaim(string type);
}
