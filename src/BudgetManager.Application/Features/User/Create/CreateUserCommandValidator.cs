using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.User.Create;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private readonly IUserManager _userManager;

    public CreateUserCommandValidator(IUserManager userManager)
    {
        _userManager = userManager;

        RuleFor(x => x.NormalizedEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(command => "Email is required.")
                .WithErrorCode(ErrorCodes.UserEmailRequired)
            .MaximumLength(Domain.Common.StringPropertyLengths.EmailLength)
                .WithMessage("Email is too long.")
                .WithErrorCode(ErrorCodes.UserEmailTooLong)
            .Must(email =>
                email.All(c =>
                    char.IsAsciiLetterOrDigit(c)
                    || c is '-' or '.' or '_' or '@' or '+'))
                .WithMessage("Email contains unsupported characters.")
                .WithErrorCode(ErrorCodes.UserEmailInvalidCharacters)
            .EmailAddress()
                .WithMessage("Email is invalid.")
                .WithErrorCode(ErrorCodes.UserEmailInvalid)
            .MustAsync(EmailMustBeUnique)
                .WithMessage(command => "Email is already used.")
                .WithErrorCode(ErrorCodes.UserEmailAlreadyUsed);

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(command => "LastName is required.")
                .WithErrorCode(ErrorCodes.UserLastNameRequired)
            .MaximumLength(Domain.Common.StringPropertyLengths.LastNameLength)
                .WithMessage("LastName is too long.")
                .WithErrorCode(ErrorCodes.UserLastNameTooLong);

        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(command => "FirstName is required.")
                .WithErrorCode(ErrorCodes.UserFirstNameRequired)
            .MaximumLength(Domain.Common.StringPropertyLengths.FirstNameLength)
                .WithMessage("FirstName is too long.")
                .WithErrorCode(ErrorCodes.UserFirstNameTooLong);

        RuleFor(x => x.PhoneNumberE164)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(Domain.Common.StringPropertyLengths.PhoneNumberLength)
                .WithMessage("PhoneNumber is too long.")
                .WithErrorCode(ErrorCodes.UserPhoneNumberTooLong)
            .Matches(@"^\+[1-9]\d{1,14}$")
                .WithMessage("PhoneNumber is invalid.")
                .WithErrorCode(ErrorCodes.UserPhoneNumberInvalid)
            .When(x => x.PhoneNumberE164 is not null, ApplyConditionTo.AllValidators);

        RuleFor(x => x.Roles)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("At least one role is required.")
                .WithErrorCode(ErrorCodes.UserRoleRequired)
            .Must(roles => roles.Distinct(StringComparer.OrdinalIgnoreCase).Count() == roles.Count)
                .WithMessage("Roles must be unique.")
                .WithErrorCode(ErrorCodes.UserRoleDuplicated)
            .DependentRules(() =>
            {
                RuleForEach(x => x.Roles)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                        .WithMessage("Role is required.")
                        .WithErrorCode(ErrorCodes.UserRoleRequired)
                    .Must(role => ApplicationRoles.All.Any(x => string.Equals(x, role, StringComparison.OrdinalIgnoreCase)))
                        .WithMessage("Role does not exist.")
                        .WithErrorCode(ErrorCodes.UserRoleNotExists)
                    .MustAsync(async (role, cancellationToken) => await userManager.RoleExistsAsync(role, cancellationToken))
                        .WithMessage("Role does not exist.")
                        .WithErrorCode(ErrorCodes.UserRoleNotExists);
            });

        RuleFor(x => x.PreferredCulture)
            .NotEmpty()
            .Must(SupportedCultures.All.Contains)
            .WithMessage("The selected culture is not supported.")
            .WithErrorCode(ErrorCodes.CultureNotSupported);

        RuleFor(x => x.PreferredTheme)
            .Must(theme => theme is null || SupportedThemes.All.Contains(theme))
            .WithMessage("The selected theme is not supported.")
            .WithErrorCode(ErrorCodes.ThemeNotSupported);

        When(x => x.ProfilePicture is not null, () =>
        {
            RuleFor(x => x.ProfilePicture!.Content)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithMessage("Profile picture content is required.")
                    .WithErrorCode(ErrorCodes.UserProfilePictureContentRequired)
                .Must(content => content.Length <= 500 * 1024)
                    .WithMessage("Profile picture must not exceed 500 KB.")
                    .WithErrorCode(ErrorCodes.UserProfilePictureTooLarge);

            RuleFor(x => x.ProfilePicture!.ContentType)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithMessage("Profile picture content type is required.")
                    .WithErrorCode(ErrorCodes.UserProfilePictureContentTypeRequired)
                .Must(contentType => SupportedPictureFormats.All.Any(x => string.Equals(x.ContentType, contentType, StringComparison.OrdinalIgnoreCase)))
                    .WithMessage("Unsupported profile picture format.")
                    .WithErrorCode(ErrorCodes.UserProfilePictureFormatUnsupported);
        });

        RuleFor(x => x.ActivationPageName)
            .NotEmpty()
                .WithMessage("ActivationPageName is required.")
                .WithErrorCode(ErrorCodes.UserActivationPageNameRequired);

        RuleFor(x => x.ActivationEmailSubject)
            .NotEmpty()
                .WithMessage("Activation email subject is required.")
                .WithErrorCode(ErrorCodes.UserActivationEmailSubjectRequired);

        RuleFor(x => x.ActivationTokenLifetime)
            .GreaterThan(TimeSpan.Zero)
                .WithMessage("ActivationTokenLifetime must be greater than zero.")
                .WithErrorCode(ErrorCodes.UserActivationTokenLifetimeInvalid);
    }

    private async Task<bool> EmailMustBeUnique(string email, CancellationToken cancellationToken)
    {
        return !await _userManager.EmailExistsAsync(email, null, cancellationToken);
    }
}
