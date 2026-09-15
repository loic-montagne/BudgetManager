using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Exceptions;
using MediatR;

namespace BudgetManager.Application.Behaviors;

/// <summary>
/// Authenticates and authorizes requests implementing <see cref="IAuthorizedRequest"/> before invoking their handlers.
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IAuthorizedRequest authorizedRequest)
        {
            UnauthenticatedException.ThrowIfUnauthenticated(currentUser);
            ForbiddenAccessException.ThrowIfForbiddenAccess(currentUser, authorizedRequest.RequiredRoles);
        }

        return await next(cancellationToken);
    }
}
