using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.User.SendActivationEmail;

public sealed class SendUserActivationEmailCommandValidator : AbstractValidator<SendUserActivationEmailCommand>
{
    private readonly IUserContext _userContext;

    public SendUserActivationEmailCommandValidator(IUserContext userContext)
    {
        _userContext = userContext;
        
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.UserIdRequired)
            .MustAsync(userContext.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.UserNotExists)
            .MustAsync(EmailIsNotConfirmedAsync)
                .WithMessage("User email is already confirmed.")
                .WithErrorCode(ErrorCodes.UserEmailAlreadyConfirmed);

        RuleFor(x => x.ActivationPageName)
            .NotEmpty()
                .WithMessage("ActivationPageName is required.")
                .WithErrorCode(ErrorCodes.UserActivationPageNameRequired);

        RuleFor(x => x.Subject)
            .NotEmpty()
                .WithMessage("Subject is required.")
                .WithErrorCode(ErrorCodes.UserActivationEmailSubjectRequired);

        RuleFor(x => x.TokenLifetime)
            .GreaterThan(TimeSpan.Zero)
                .WithMessage("TokenLifetime must be greater than zero.")
                .WithErrorCode(ErrorCodes.UserActivationTokenLifetimeInvalid);
    }

    private async Task<bool> EmailIsNotConfirmedAsync(Guid id, CancellationToken cancellationToken)
    {
        return !(await _userContext.GetRequiredAsync(id, cancellationToken)).EmailConfirmed;
    }
}
