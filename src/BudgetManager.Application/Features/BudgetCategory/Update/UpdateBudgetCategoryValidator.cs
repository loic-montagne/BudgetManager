using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.BudgetCategory.Update;

public sealed class UpdateBudgetCategoryValidator : AbstractValidator<UpdateBudgetCategoryCommand>
{
    private readonly IBudgetCategoryRepository _budgetCategoryRepository;

    public UpdateBudgetCategoryValidator(IBudgetCategoryContext budgetCategoryContext, IBudgetCategoryRepository budgetCategoryRepository)
    {
        _budgetCategoryRepository = budgetCategoryRepository;

        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.BudgetCategoryIdRequired)
            .MustAsync(budgetCategoryContext.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.BudgetCategoryNotExists);

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

    private async Task<bool> NameMustBeUnique(UpdateBudgetCategoryCommand command, string name, CancellationToken cancellationToken)
    {
        return await _budgetCategoryRepository.IsNameUniqueAsync(name.Trim(), command.Id, cancellationToken);
    }

}
