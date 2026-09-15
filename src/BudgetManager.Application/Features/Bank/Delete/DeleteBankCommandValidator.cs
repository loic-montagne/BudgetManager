using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.Bank.Delete;

public sealed class DeleteBankCommandValidator : AbstractValidator<DeleteBankCommand>
{
    private readonly IBankRepository _bankRepository;

    public DeleteBankCommandValidator(IBankContext bankContext, IBankRepository bankRepository)
    {
        _bankRepository = bankRepository;

        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.BankIdRequired)
            .MustAsync(bankContext.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.BankNotExists)
            .MustAsync(BankMustBeUnused)
                .WithMessage("Bank is used and cannot be deleted.")
                .WithErrorCode(ErrorCodes.BankIsUsed);
    }

    private async Task<bool> BankMustBeUnused(Guid id, CancellationToken cancellationToken)
    {
        return !await _bankRepository.IsUsedAsync(id, cancellationToken);
    }

}
