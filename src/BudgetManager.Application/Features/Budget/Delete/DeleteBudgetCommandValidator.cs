using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Domain.Enums;
using FluentValidation;

namespace BudgetManager.Application.Features.Budget.Delete;

public sealed class DeleteBudgetCommandValidator : AbstractValidator<DeleteBudgetCommand>
{
    public DeleteBudgetCommandValidator(IBudgetContext budgetContext, ICurrentUser currentUser)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.BudgetIdRequired)
            .MustAsync(budgetContext.ExistsAsync)
                .WithMessage("BudgetId does not exist.")
                .WithErrorCode(ErrorCodes.BudgetNotExists)
            .MustAsync((command, id, cancellationToken) => budgetContext.IsOwnerAsync(id, currentUser.RequiredUserId, cancellationToken))
                .WithMessage($"Current user is not the owner of budget.")
                .WithErrorCode(ErrorCodes.BudgetPermissionInvalid);
    }

}
