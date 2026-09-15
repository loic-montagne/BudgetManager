using BudgetManager.Application.Abstractions.Identity;
using MediatR;

namespace BudgetManager.Application.Features.User.Delete;

public sealed class DeleteUserCommandHandler(IUserManager userManager) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await userManager.DeleteAsync(request.Id, cancellationToken);
    }
}
