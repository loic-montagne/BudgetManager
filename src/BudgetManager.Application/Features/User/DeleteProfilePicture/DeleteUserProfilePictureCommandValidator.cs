using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.User.DeleteProfilePicture;

public sealed class DeleteUserProfilePictureCommandValidator : AbstractValidator<DeleteUserProfilePictureCommand>
{
    public DeleteUserProfilePictureCommandValidator(IUserManager userManager, ICurrentUser currentUser)
    {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("UserId is required.")
                .WithErrorCode(ErrorCodes.UserIdRequired)
            .MustAsync(userManager.ExistsAsync)
                .WithMessage("User does not exist.")
                .WithErrorCode(ErrorCodes.UserNotExists)
            .Equal(currentUser.RequiredUserId)
                .WithMessage("Cannot update another user.")
                .WithErrorCode(ErrorCodes.UserNotCurrent)
            .MustAsync(userManager.ProfilePictureExistsAsync)
                .WithMessage("User profile picture does not exist.")
                .WithErrorCode(ErrorCodes.UserProfilePictureNotExists);
    }
}
