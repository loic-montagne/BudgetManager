using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.Account.Delete;

public sealed class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    private readonly IAccountRepository _accountRepository;
    public DeleteAccountCommandValidator(IAccountContext accountContext, IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;

        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.AccountIdRequired)
            .MustAsync(accountContext.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.AccountNotExists)
            .MustAsync(AccountMustBeUnused)
                .WithMessage("Account is used and cannot be deleted.")
                .WithErrorCode(ErrorCodes.AccountIsUsed);
    }

    private async Task<bool> AccountMustBeUnused(Guid id, CancellationToken cancellationToken)
    {
        return !await _accountRepository.IsUsedAsync(id, cancellationToken);
    }

}
