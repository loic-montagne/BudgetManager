using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.User.UpdateProfilePicture;

public sealed class UpdateUserProfilePictureCommandValidator : AbstractValidator<UpdateUserProfilePictureCommand>
{
    public UpdateUserProfilePictureCommandValidator(IUserManager userManager, ICurrentUser currentUser)
    {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("UserId is required.")
                .WithErrorCode(ErrorCodes.UserIdRequired)
            .MustAsync(userManager.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.UserNotExists)
            .Equal(currentUser.RequiredUserId)
                .WithMessage("Cannot update another user.")
                .WithErrorCode(ErrorCodes.UserNotCurrent);

        RuleFor(x => x.ProfilePicture)
            .NotNull()
                .WithMessage("ProfilePicture is required.")
                .WithErrorCode(ErrorCodes.UserProfilePictureRequired)
            .DependentRules(() =>
            {
                RuleFor(x => x.ProfilePicture.Content)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                        .WithMessage("Profile picture content is required.")
                        .WithErrorCode(ErrorCodes.UserProfilePictureContentRequired)
                    .Must(content => content.Length <= 500 * 1024)
                        .WithMessage("Profile picture must not exceed 500 KB.")
                        .WithErrorCode(ErrorCodes.UserProfilePictureTooLarge);

                RuleFor(x => x.ProfilePicture.ContentType)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                        .WithMessage("Profile picture content type is required.")
                        .WithErrorCode(ErrorCodes.UserProfilePictureContentTypeRequired)
                    .Must(contentType => SupportedPictureFormats.All.Any(x => string.Equals(x.ContentType, contentType, StringComparison.OrdinalIgnoreCase)))
                        .WithMessage("Unsupported profile picture format.")
                        .WithErrorCode(ErrorCodes.UserProfilePictureFormatUnsupported);

            });
    }
}
