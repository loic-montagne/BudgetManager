using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Domain.Enums;
using BudgetManager.Domain.Extensions;
using FluentValidation;

namespace BudgetManager.Application.Features.Budget.UpdateAccess;

public sealed class UpdateBudgetAccessCommandValidator : AbstractValidator<UpdateBudgetAccessCommand>
{
    public UpdateBudgetAccessCommandValidator(IBudgetContext budgetContext, IUserContext userContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("UserId is required.")
                .WithErrorCode(ErrorCodes.BudgetUserRequired)
            .MustAsync(userContext.ExistsAsync)
                .WithMessage("UserId must exist.")
                .WithErrorCode(ErrorCodes.BudgetUserNotExists);

        RuleFor(x => x.BudgetId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("BudgetId is required.")
                .WithErrorCode(ErrorCodes.BudgetIdRequired)
            .MustAsync(budgetContext.ExistsAsync)
                .WithMessage("BudgetId does not exist.")
                .WithErrorCode(ErrorCodes.BudgetNotExists)
            .DependentRules(() =>
            {
                RuleFor(x => x.BudgetId)
                    .MustAsync((command, budgetId, cancellationToken) =>
                        budgetContext.HasCurrentUserPermissionAsync(
                            budgetId,
                            Permission.Share,
                            cancellationToken))
                        .WithMessage($"Current user does not have permission '{Permission.Share}' on budget.")
                        .WithErrorCode(ErrorCodes.BudgetPermissionInvalid);

                RuleFor(x => x.UserId)
                    .MustAsync((command, userId, cancellationToken) =>
                        budgetContext.IsNotOwnerAsync(
                            command.BudgetId,
                            userId,
                            cancellationToken))
                        .When(
                            command => command.UserId != Guid.Empty,
                            ApplyConditionTo.CurrentValidator)
                            .WithMessage("User is the owner of budget.")
                            .WithErrorCode(ErrorCodes.BudgetUserIsOwner);
            });

        RuleFor(x => x.Permissions)
            .Must(permissions => permissions.IsValid())
                .WithMessage("Permissions must exist.")
                .WithErrorCode(ErrorCodes.BudgetPermissionsInvalid);
    }
}
