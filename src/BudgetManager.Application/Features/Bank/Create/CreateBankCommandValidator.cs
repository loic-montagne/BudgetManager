using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Features.Bank.Common;
using BudgetManager.Domain.ValueObjects;
using FluentValidation;

namespace BudgetManager.Application.Features.Bank.Create;

public sealed class CreateBankCommandValidator : AbstractValidator<CreateBankCommand>
{
    private readonly IBankRepository _bankRepository;

    public CreateBankCommandValidator(IBankRepository bankRepository)
    {
        _bankRepository = bankRepository;

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(command => "Name is required.")
                .WithErrorCode(ErrorCodes.BankNameRequired)
            .MaximumLength(Domain.Common.StringPropertyLengths.NameLength)
                .WithMessage("Name is too long.")
                .WithErrorCode(ErrorCodes.BankNameTooLong)
            .MustAsync(NameMustBeUnique)
                .WithMessage(command => "Name is already used.")
                .WithErrorCode(ErrorCodes.BankNameAlreadyUsed);

        RuleFor(x => x.Bic)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("BIC is required.")
                .WithErrorCode(ErrorCodes.BankBicRequired)
            .Custom(BicValidatorAdapter.ValidateBic)
            .MustAsync(BicMustBeUnique)
                .WithMessage("BIC is already used.")
                .WithErrorCode(ErrorCodes.BankBicAlreadyUsed);
    }

    private async Task<bool> NameMustBeUnique(string name, CancellationToken cancellationToken)
    {
        return await _bankRepository.IsNameUniqueAsync(name.Trim(), null, cancellationToken);
    }

    private async Task<bool> BicMustBeUnique(string bic, CancellationToken cancellationToken)
    {
        var obj = Bic.Create(bic);
        return await _bankRepository.IsBicUniqueAsync(obj, null, cancellationToken);
    }

}
