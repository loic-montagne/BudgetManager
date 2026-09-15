using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Extensions;
using FluentValidation;

namespace BudgetManager.Application.Features.Budget.Update;

public sealed class UpdateBudgetCommandValidator : AbstractValidator<UpdateBudgetCommand>
{
    private readonly IBudgetRepository _budgetRepository;

    public UpdateBudgetCommandValidator(IBudgetRepository budgetRepository, IBudgetContext budgetContext)
    {
        _budgetRepository = budgetRepository;

        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.BudgetIdRequired)
            .MustAsync(budgetContext.ExistsAsync)
                .WithMessage("BudgetId does not exist.")
                .WithErrorCode(ErrorCodes.BudgetNotExists)
            .CustomAsync(async (id, context, cancellationToken)
                => (await budgetContext.IsEditableAsync(id, cancellationToken))
                                       .GetBudgetEditableStatusError());

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Name is required.")
                .WithErrorCode(ErrorCodes.BudgetNameRequired)
            .MaximumLength(Domain.Common.StringPropertyLengths.NameLength)
                .WithMessage("Name is too long.")
                .WithErrorCode(ErrorCodes.BudgetNameTooLong)
            .MustAsync(NameMustBeUnique)
                .WithMessage("Name is already used.")
                .WithErrorCode(ErrorCodes.BudgetNameAlreadyUsed);
    }

    private async Task<bool> NameMustBeUnique(UpdateBudgetCommand command, string name, CancellationToken cancellationToken)
    {
        return await _budgetRepository.IsNameUniqueAsync(name.Trim(), command.Id, cancellationToken);
    }
}
