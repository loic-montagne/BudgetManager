using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Features.Bank.Common;
using BudgetManager.Domain.ValueObjects;
using FluentValidation;

namespace BudgetManager.Application.Features.Bank.Update;

public sealed class UpdateBankCommandValidator : AbstractValidator<UpdateBankCommand>
{
    private readonly IBankRepository _bankRepository;

    public UpdateBankCommandValidator(IBankContext bankContext, IBankRepository bankRepository)
    {
        _bankRepository = bankRepository;

        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.BankIdRequired)
            .MustAsync(bankContext.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.BankNotExists);

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Name is required.")
                .WithErrorCode(ErrorCodes.BankNameRequired)
            .MaximumLength(Domain.Common.StringPropertyLengths.NameLength)
                .WithMessage("Name is too long.")
                .WithErrorCode(ErrorCodes.BankNameTooLong)
            .MustAsync(NameMustBeUnique)
                .WithMessage("Name is already used.")
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

    private async Task<bool> NameMustBeUnique(UpdateBankCommand command, string name, CancellationToken cancellationToken)
    {
        return await _bankRepository.IsNameUniqueAsync(name.Trim(), command.Id, cancellationToken);
    }

    private async Task<bool> BicMustBeUnique(UpdateBankCommand command, string bic, CancellationToken cancellationToken)
    {
        var obj = Bic.Create(bic);
        return await _bankRepository.IsBicUniqueAsync(obj, command.Id, cancellationToken);
    }

}
