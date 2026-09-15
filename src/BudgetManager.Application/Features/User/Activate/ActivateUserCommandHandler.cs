using BudgetManager.Application.Abstractions.Identity;
using MediatR;

namespace BudgetManager.Application.Features.User.Activate;

public sealed class ActivateUserCommandHandler(IUserManager userManager) : IRequestHandler<ActivateUserCommand>
{
    public async Task Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        await userManager.ActivateAsync(request.Id, request.ActivationToken, request.Password, cancellationToken);
    }
}
