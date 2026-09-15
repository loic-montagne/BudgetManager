using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.User.UpdateUiPreferences;

public sealed class UpdateUiPreferencesCommandValidator : AbstractValidator<UpdateUiPreferencesCommand>
{
    public UpdateUiPreferencesCommandValidator(IUserManager userManager, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.UserIdRequired)
            .MustAsync(userManager.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.UserNotExists)
            .Equal(currentUser.RequiredUserId)
                .WithMessage("Cannot update another user.")
                .WithErrorCode(ErrorCodes.UserNotCurrent);

        RuleFor(x => x.PreferredCulture)
            .NotEmpty()
            .Must(SupportedCultures.All.Contains)
            .WithMessage("The selected culture is not supported.")
            .WithErrorCode(ErrorCodes.CultureNotSupported);

        RuleFor(x => x.PreferredTheme)
            .Must(theme => theme is null || SupportedThemes.All.Contains(theme))
            .WithMessage("The selected theme is not supported.")
            .WithErrorCode(ErrorCodes.ThemeNotSupported);
    }
}
