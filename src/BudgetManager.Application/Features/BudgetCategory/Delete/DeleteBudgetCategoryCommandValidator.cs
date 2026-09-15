using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.BudgetCategory.Delete;

public sealed class DeleteBudgetCategoryCommandValidator : AbstractValidator<DeleteBudgetCategoryCommand>
{
    private readonly IBudgetCategoryRepository _budgetCategoryRepository;

    public DeleteBudgetCategoryCommandValidator(IBudgetCategoryContext budgetCategoryContext, IBudgetCategoryRepository budgetCategoryRepository)
    {
        _budgetCategoryRepository = budgetCategoryRepository;

        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.BudgetCategoryIdRequired)
            .MustAsync(budgetCategoryContext.ExistsAsync)
                .WithMessage("Id does not exist.")
                .WithErrorCode(ErrorCodes.BudgetCategoryNotExists)
            .MustAsync(BudgetCategoryMustBeUnused)
                .WithMessage("Budget category is used and cannot be deleted.")
                .WithErrorCode(ErrorCodes.BudgetCategoryIsUsed);
    }

    private async Task<bool> BudgetCategoryMustBeUnused(Guid id, CancellationToken cancellationToken)
    {
        return !await _budgetCategoryRepository.IsUsedAsync(id, cancellationToken);
    }

}
