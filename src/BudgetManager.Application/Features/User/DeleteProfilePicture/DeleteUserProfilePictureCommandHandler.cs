using BudgetManager.Application.Abstractions.Identity;
using MediatR;

namespace BudgetManager.Application.Features.User.DeleteProfilePicture;

public sealed class DeleteUserProfilePictureCommandHandler(IUserManager userManager) : IRequestHandler<DeleteUserProfilePictureCommand>
{
    public async Task Handle(DeleteUserProfilePictureCommand request, CancellationToken cancellationToken)
    {
        await userManager.DeleteProfilePictureAsync(request.UserId, cancellationToken);
    }
}
