using BudgetManager.Application.Abstractions.Authentication;

namespace BudgetManager.Application.Exceptions;

public sealed class ForbiddenAccessException : Common.ApplicationException
{
    public ForbiddenAccessException(ICurrentUser currentUser, params IEnumerable<string> authorizedRoles)
        : base(authorizedRoles?.Count() == 1 ?
            $"User {currentUser.UserName} ({currentUser.UserId}) must be in {authorizedRoles.FirstOrDefault()} role." : 
            $"User {currentUser.UserName} ({currentUser.UserId}) must be in at least one of these roles: {string.Join(", ", authorizedRoles ?? [])}.")
    {
    }

    internal static void ThrowIfForbiddenAccess(ICurrentUser currentUser, params IEnumerable<string> authorizedRoles)
    {
        if (!authorizedRoles.Any(currentUser.IsInRole))
            throw new ForbiddenAccessException(currentUser, authorizedRoles);
    }
}