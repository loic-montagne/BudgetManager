using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.Budget.TransferOwnership;

public sealed class TransferBudgetOwnershipCommandValidator : AbstractValidator<TransferBudgetOwnershipCommand>
{
    public TransferBudgetOwnershipCommandValidator(IBudgetContext budgetContext, IUserContext userContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("UserId is required.")
                .WithErrorCode(ErrorCodes.BudgetUserRequired)
            .NotEqual(currentUser.RequiredUserId)
                .WithMessage("User can not transfer ownership to himself.")
                .WithErrorCode(ErrorCodes.BudgetUserIsCurrent)
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
                        budgetContext.IsOwnerAsync(
                            budgetId,
                            currentUser.RequiredUserId,
                            cancellationToken))
                        .WithMessage($"Current user is not the owner of budget.")
                        .WithErrorCode(ErrorCodes.BudgetUserIsNotOwner);

                RuleFor(x => x.UserId)
                    .MustAsync((command, userId, cancellationToken) =>
                        budgetContext.IsNotOwnerAsync(
                            command.BudgetId,
                            userId,
                            cancellationToken))
                        .When(
                            command =>
                                command.UserId != Guid.Empty &&
                                command.UserId != currentUser.RequiredUserId,
                            ApplyConditionTo.CurrentValidator)
                            .WithMessage($"User is already the owner of budget.")
                            .WithErrorCode(ErrorCodes.BudgetUserIsOwner);
            });
    }
}
