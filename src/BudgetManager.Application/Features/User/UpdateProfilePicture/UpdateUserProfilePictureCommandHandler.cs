using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Exceptions;
using MediatR;

namespace BudgetManager.Application.Features.User.UpdateProfilePicture;

public sealed class UpdateUserProfilePictureCommandHandler(IUserManager userManager, IProfilePictureProcessor profilePictureProcessor) : IRequestHandler<UpdateUserProfilePictureCommand>
{
    public async Task Handle(UpdateUserProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var result = profilePictureProcessor.Process(request.ProfilePicture, cancellationToken);
        BadRequestException.ThrowIfResultIsNotValid(result, nameof(request.ProfilePicture));
        var processedPicture = result.Value;
        await userManager.UpdateProfilePictureAsync(request.UserId, processedPicture, cancellationToken);
    }
}
