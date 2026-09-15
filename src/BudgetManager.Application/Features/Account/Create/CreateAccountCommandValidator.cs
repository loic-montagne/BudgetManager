using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Features.Account.Common;
using BudgetManager.Domain.ValueObjects;
using FluentValidation;

namespace BudgetManager.Application.Features.Account.Create;

public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    private readonly IAccountRepository _accountRepository;

    public CreateAccountCommandValidator(IAccountRepository accountRepository, IBankContext bankContext)
    {
        _accountRepository = accountRepository;

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

        RuleFor(x => x.BankId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Bank is required.")
                .WithErrorCode(ErrorCodes.AccountBankRequired)
            .MustAsync(bankContext.ExistsAsync)
                .WithMessage("Bank must exist.")
                .WithErrorCode(ErrorCodes.AccountBankNotExists); 

        RuleFor(x => x.Iban)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("IBAN is required.")
                .WithErrorCode(ErrorCodes.AccountIbanRequired)
            .Custom(IbanValidatorAdapter.ValidateIban)
            .MustAsync(IbanMustBeUnique)
                .WithMessage("IBAN is already used.")
                .WithErrorCode(ErrorCodes.AccountIbanAlreadyUsed);
    }

    private async Task<bool> NameMustBeUnique(string name, CancellationToken cancellationToken)
    {
        return await _accountRepository.IsNameUniqueAsync(name.Trim(), null, cancellationToken);
    }
    private async Task<bool> IbanMustBeUnique(string iban, CancellationToken cancellationToken)
    {
        var obj = Iban.Create(iban);
        return await _accountRepository.IsIbanUniqueAsync(obj, null, cancellationToken);
    }
}
