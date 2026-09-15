using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common.Errors;
using FluentValidation;
using FluentValidation.Results;

namespace BudgetManager.Application.Features.User.Activate;

public sealed class ActivateUserCommandValidator : AbstractValidator<ActivateUserCommand>
{
    private readonly IUserContext _userContext;

    public ActivateUserCommandValidator(IUserContext userContext, IPasswordValidator passwordValidator)
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

        RuleFor(x => x.ActivationToken)
            .NotEmpty()
                .WithMessage(command => "ActivationToken is required.")
                .WithErrorCode(ErrorCodes.UserActivationTokenRequired);

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(command => "Password is required.")
                .WithErrorCode(ErrorCodes.UserPasswordRequired)
            .CustomAsync(async (password, context, cancellationToken) =>
            {
                if (context.InstanceToValidate.Id == Guid.Empty)
                    return;

                var user = await userContext.GetAsync(context.InstanceToValidate.Id, cancellationToken);
                if (user is null)
                    return;

                var r = await passwordValidator.ValidateAsync(user.Email, user.ToUserData(), password, cancellationToken);
                if (!r.IsValid)
                {
                    foreach (var error in r.Errors)
                    {
                        context.AddFailure(new ValidationFailure(context.PropertyPath, $"{error.Message} ({error.Code}).")
                        {
                            ErrorCode = ErrorCodes.UserPasswordInvalid
                        });
                    }
                }
            })
            .Equal(x => x.ConfirmPassword)
                .WithMessage(command => "Password and ConfirmPassword are not equal.")
                .WithErrorCode(ErrorCodes.UserPasswordNotEqualToConfirm);
    }

    private async Task<bool> EmailIsNotConfirmedAsync(Guid id, CancellationToken cancellationToken)
    {
        return !(await _userContext.GetRequiredAsync(id, cancellationToken)).EmailConfirmed;
    }
}
