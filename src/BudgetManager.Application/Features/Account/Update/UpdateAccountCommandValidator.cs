using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.Account.Update;

public sealed class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    private readonly IAccountRepository _accountRepository;

    public UpdateAccountCommandValidator(IAccountContext accountContext, IAccountRepository accountRepository)
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
            .MustAsync(accountContext.IsOpenedAsync)
                .WithMessage("Account is already closed.")
                .WithErrorCode(ErrorCodes.AccountIsAlreadyClosed);

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Name is required.")
                .WithErrorCode(ErrorCodes.AccountNameRequired)
            .MaximumLength(Domain.Common.StringPropertyLengths.NameLength)
                .WithMessage("Name is too long.")
                .WithErrorCode(ErrorCodes.AccountNameTooLong)
            .MustAsync(NameMustBeUnique)
                .WithMessage("Name is already used.")
                .WithErrorCode(ErrorCodes.AccountNameAlreadyUsed);
    }

    private async Task<bool> NameMustBeUnique(UpdateAccountCommand command, string name, CancellationToken cancellationToken)
    {
        return await _accountRepository.IsNameUniqueAsync(name.Trim(), command.Id, cancellationToken);
    }

}
