using BudgetManager.Application.Abstractions.Identity;
using MediatR;

namespace BudgetManager.Application.Features.User.ConfirmEmailChange;

public sealed class ConfirmEmailChangeUserCommandHandler(IUserManager userManager) : IRequestHandler<ConfirmEmailChangeUserCommand>
{
    public async Task Handle(ConfirmEmailChangeUserCommand request, CancellationToken cancellationToken)
    {
        await userManager.ConfirmEmailChangeAsync(request.Id, request.NormalizedEmail, request.Token, cancellationToken);
    }
}
