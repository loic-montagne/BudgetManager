using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.User.Update;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator(IUserManager userManager)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.UserIdRequired)
            .MustAsync(userManager.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.UserNotExists);

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

                RuleFor(x => x)
                    .MustAsync(async (command, cancellationToken) =>
                    {
                        if (command.Roles.Any(x => string.Equals(x, ApplicationRoles.Administrator, StringComparison.OrdinalIgnoreCase)))
                            return true;
                        return !await userManager.IsLastActivatedAdministratorAsync(command.Id, cancellationToken);
                    })
                    .WithMessage("The administrator role cannot be removed from the last activated administrator.")
                    .WithErrorCode(ErrorCodes.UserIsLastActivatedAdministrator);
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
    }
}
