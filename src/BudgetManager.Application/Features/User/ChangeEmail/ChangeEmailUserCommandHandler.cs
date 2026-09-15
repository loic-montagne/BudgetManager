using BudgetManager.Application.Abstractions.Identity;
using MediatR;

namespace BudgetManager.Application.Features.User.ChangeEmail;

public sealed class ChangeEmailUserCommandHandler(IUserManager userManager) : IRequestHandler<ChangeEmailUserCommand, string>
{
    public async Task<string> Handle(ChangeEmailUserCommand request, CancellationToken cancellationToken)
    {
        return await userManager.ChangeEmailAsync(request.Id, request.NormalizedNewEmail, cancellationToken);
    }
}
