using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.User.ConfirmEmailChange;

public sealed class ConfirmEmailChangeUserCommandValidator : AbstractValidator<ConfirmEmailChangeUserCommand>
{
    private readonly IUserManager _userManager;

    public ConfirmEmailChangeUserCommandValidator(IUserManager userManager)
    {
        _userManager = userManager;

        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.UserIdRequired)
            .MustAsync(userManager.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.UserNotExists);

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

        RuleFor(x => x.Token)
            .NotEmpty()
                .WithMessage("Token is required.")
                .WithErrorCode(ErrorCodes.TokenRequired);
    }

    private async Task<bool> EmailMustBeUnique(ConfirmEmailChangeUserCommand command, string email, CancellationToken cancellationToken)
    {
        return !await _userManager.EmailExistsAsync(email, command.Id, cancellationToken);
    }
}
