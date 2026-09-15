using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.User.Delete;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator(IUserManager userManager, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.UserIdRequired)
            .MustAsync(userManager.ExistsAsync)
                .WithMessage("User does not exist.")
                .WithErrorCode(ErrorCodes.UserNotExists)
            .Must(id => id != currentUser.RequiredUserId)
                .WithMessage("User is current user.")
                .WithErrorCode(ErrorCodes.UserIsCurrentUser)
            .MustAsync(async (id, cancellationToken) => !await userManager.IsBudgetOwnerAsync(id, cancellationToken))
                .WithMessage($"User is the owner of one or more budget.")
                .WithErrorCode(ErrorCodes.UserIsBudgetOwner)
            .MustAsync(async (id, cancellationToken) => !await userManager.IsLastActivatedAdministratorAsync(id, cancellationToken))
                .WithMessage("The last activated administrator cannot be deleted.")
                .WithErrorCode(ErrorCodes.UserIsLastActivatedAdministrator);
    }
}
