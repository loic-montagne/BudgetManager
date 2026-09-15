using BudgetManager.Application.Abstractions.Identity;
using MediatR;

namespace BudgetManager.Application.Features.User.UpdateProfile;

public sealed class UpdateUserProfileCommandHandler(IUserManager userManager) : IRequestHandler<UpdateUserProfileCommand>
{
    public async Task Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userData = request.ToUserData();
        await userManager.UpdateProfileAsync(request.Id, userData, cancellationToken);
    }
}
