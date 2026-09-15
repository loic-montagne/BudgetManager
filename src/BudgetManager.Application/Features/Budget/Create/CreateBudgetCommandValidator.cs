using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.Budget.Create;

public sealed class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    private readonly IBudgetRepository _budgetRepository;

    public CreateBudgetCommandValidator(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(command => "Name is required.")
                .WithErrorCode(ErrorCodes.BudgetNameRequired)
            .MaximumLength(Domain.Common.StringPropertyLengths.NameLength)
                .WithMessage("Name is too long.")
                .WithErrorCode(ErrorCodes.BudgetNameTooLong)
            .MustAsync(NameMustBeUnique)
                .WithMessage(command => "Name is already used.")
                .WithErrorCode(ErrorCodes.BudgetNameAlreadyUsed);
    }

    private async Task<bool> NameMustBeUnique(string name, CancellationToken cancellationToken)
    {
        return await _budgetRepository.IsNameUniqueAsync(name.Trim(), null, cancellationToken);
    }
}
