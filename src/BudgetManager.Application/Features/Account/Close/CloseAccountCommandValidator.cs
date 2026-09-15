using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.Account.Close;

public sealed class CloseAccountCommandValidator : AbstractValidator<CloseAccountCommand>
{
    public CloseAccountCommandValidator(IAccountContext accountContext)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.AccountIdRequired)
            .MustAsync(accountContext.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.AccountNotExists)
            .MustAsync(accountContext.IsOpenedAsync)
                .WithMessage("Account is already closed.")
                .WithErrorCode(ErrorCodes.AccountIsAlreadyClosed);
    }
}
