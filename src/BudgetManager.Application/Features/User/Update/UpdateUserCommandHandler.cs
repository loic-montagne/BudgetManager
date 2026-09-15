using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.Common;
using MediatR;

namespace BudgetManager.Application.Features.User.Update;

public sealed class UpdateUserCommandHandler(IUserManager userManager) : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var userData = request.ToUserData();
        await userManager.UpdateAsync(request.Id, userData, request.Roles, cancellationToken);
    }
}
