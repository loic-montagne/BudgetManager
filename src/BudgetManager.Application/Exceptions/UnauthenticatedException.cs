using BudgetManager.Application.Abstractions.Authentication;

namespace BudgetManager.Application.Exceptions;

public sealed class UnauthenticatedException : Common.ApplicationException
{
    public UnauthenticatedException(ICurrentUser currentUser) : base($"User {currentUser.UserName} ({currentUser.UserId}) is not authenticated.")
    {
    }

    internal static void ThrowIfUnauthenticated(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated)
            throw new UnauthenticatedException(currentUser);
    }
}