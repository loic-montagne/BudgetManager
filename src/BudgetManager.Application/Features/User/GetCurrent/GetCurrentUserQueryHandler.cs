using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Exceptions;
using MediatR;

namespace BudgetManager.Application.Features.User.GetCurrent;

public sealed class GetCurrentUserQueryHandler(IUserQueries queries, ICurrentUser currentUser) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await queries.GetCurrentAsync(currentUser, cancellationToken);
        NotFoundException<CurrentUserDto>.ThrowIfNull(user, currentUser.RequiredUserId);
        return user!;
    }
}
