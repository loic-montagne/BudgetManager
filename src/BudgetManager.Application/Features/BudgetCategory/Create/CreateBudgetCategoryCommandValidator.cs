using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.BudgetCategory.Create;

public sealed class CreateBudgetCategoryCommandValidator : AbstractValidator<CreateBudgetCategoryCommand>
{
    private readonly IBudgetCategoryRepository _budgetCategoryRepository;

    public CreateBudgetCategoryCommandValidator(IBudgetCategoryRepository budgetCategoryRepository)
    {
        _budgetCategoryRepository = budgetCategoryRepository;

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage(command => "Name is required.")
                .WithErrorCode(ErrorCodes.BudgetCategoryNameRequired)
            .MaximumLength(Domain.Common.StringPropertyLengths.NameLength)
                .WithMessage("Name is too long.")
                .WithErrorCode(ErrorCodes.BudgetCategoryNameTooLong)
            .MustAsync(NameMustBeUnique)
                .WithMessage(command => "Name is already used.")
                .WithErrorCode(ErrorCodes.BudgetCategoryNameAlreadyUsed);

        RuleFor(x => x.Description)
            .MaximumLength(Domain.Common.StringPropertyLengths.DescriptionLength)
                .When(x => !string.IsNullOrEmpty(x.Description?.Trim()), ApplyConditionTo.CurrentValidator)
                    .WithMessage("Description is too long.")
                    .WithErrorCode(ErrorCodes.BudgetCategoryDescriptionTooLong);
    }

    private async Task<bool> NameMustBeUnique(string name, CancellationToken cancellationToken)
    {
        return await _budgetCategoryRepository.IsNameUniqueAsync(name.Trim(), null, cancellationToken);
    }

}
