namespace BudgetManager.Application.Abstractions.Authentication;

/// <summary>
/// Marks a MediatR request that requires authentication and one of the declared application roles.
/// </summary>
public interface IAuthorizedRequest
{
    /// <summary>
    /// Gets the roles authorized to execute the request.
    /// </summary>
    IReadOnlyCollection<string> RequiredRoles { get; }
}
