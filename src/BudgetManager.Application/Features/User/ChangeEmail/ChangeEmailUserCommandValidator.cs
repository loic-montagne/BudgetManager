using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.User.ChangeEmail;

public sealed class ChangeEmailUserCommandValidator : AbstractValidator<ChangeEmailUserCommand>
{
    private readonly IUserManager _userManager;
    private readonly IUserContext _userContext;

    public ChangeEmailUserCommandValidator(IUserManager userManager, IUserContext userContext, ICurrentUser currentUser)
    {
        _userManager = userManager;
        _userContext = userContext;

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
                .WithErrorCode(ErrorCodes.UserNotCurrent)
            .MustAsync(EmailIsConfirmedAsync)
                .WithMessage("User email is not confirmed.")
                .WithErrorCode(ErrorCodes.UserEmailNotConfirmed);

        RuleFor(x => x.NormalizedOldEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(command => "OldEmail is required.")
                .WithErrorCode(ErrorCodes.UserOldEmailRequired)
            .MustAsync(OldEmailMustBeCurrent)
                .WithMessage("OldEmail is not the current user email.")
                .WithErrorCode(ErrorCodes.UserOldEmailNotCurrent);

        RuleFor(x => x.NormalizedNewEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(command => "NewEmail is required.")
                .WithErrorCode(ErrorCodes.UserNewEmailRequired)
            .NotEqual(x => x.NormalizedOldEmail)
                .When(x => !string.IsNullOrEmpty(x.NormalizedOldEmail), ApplyConditionTo.CurrentValidator)
                    .WithMessage(command => "NewEmail is equal to OldEmail.")
                    .WithErrorCode(ErrorCodes.UserNewEmailEqualsToOldEmail)
            .MaximumLength(Domain.Common.StringPropertyLengths.EmailLength)
                .WithMessage("NewEmail is too long.")
                .WithErrorCode(ErrorCodes.UserNewEmailTooLong)
            .Must(email =>
                email.All(c =>
                    char.IsAsciiLetterOrDigit(c)
                    || c is '-' or '.' or '_' or '@' or '+'))
                .WithMessage("NewEmail contains unsupported characters.")
                .WithErrorCode(ErrorCodes.UserNewEmailInvalidCharacters)
            .EmailAddress()
                .WithMessage("NewEmail is invalid.")
                .WithErrorCode(ErrorCodes.UserNewEmailInvalid)
            .MustAsync(NewEmailMustBeUnique)
                .WithMessage(command => "NewEmail is already used.")
                .WithErrorCode(ErrorCodes.UserNewEmailAlreadyUsed);
    }

    private async Task<bool> EmailIsConfirmedAsync(Guid id, CancellationToken cancellationToken)
    {
        return (await _userContext.GetRequiredAsync(id, cancellationToken)).EmailConfirmed;
    }
    private async Task<bool> OldEmailMustBeCurrent(ChangeEmailUserCommand command, string oldEmail, CancellationToken cancellationToken)
    {
        return await _userManager.EmailIsCurrentAsync(command.Id, oldEmail, cancellationToken);
    }
    private async Task<bool> NewEmailMustBeUnique(ChangeEmailUserCommand command, string newEmail, CancellationToken cancellationToken)
    {
        return !await _userManager.EmailExistsAsync(newEmail, command.Id, cancellationToken);
    }
}
