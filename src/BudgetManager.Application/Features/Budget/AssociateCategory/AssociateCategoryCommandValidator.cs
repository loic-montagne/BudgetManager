using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Extensions;
using BudgetManager.Domain.Enums;
using FluentValidation;

namespace BudgetManager.Application.Features.Budget.AssociateCategory;

public sealed class AssociateCategoryCommandValidator : AbstractValidator<AssociateCategoryCommand>
{
    public AssociateCategoryCommandValidator(IBudgetContext budgetContext, IBudgetCategoryContext categoryContext)
    {
        RuleFor(x => x.CategoryId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("CategoryId is required.")
                .WithErrorCode(ErrorCodes.BudgetBudgetCategoryRequired)
            .MustAsync(categoryContext.ExistsAsync)
                .WithMessage("CategoryId must exist.")
                .WithErrorCode(ErrorCodes.BudgetBudgetCategoryNotExists);

        RuleFor(x => x.BudgetId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("BudgetId is required.")
                .WithErrorCode(ErrorCodes.BudgetIdRequired)
            .MustAsync(budgetContext.ExistsAsync)
                .WithMessage("BudgetId does not exist.")
                .WithErrorCode(ErrorCodes.BudgetNotExists)
            .DependentRules(() =>
            {
                RuleFor(x => x.BudgetId)
                    .CustomAsync(async (id, context, cancellationToken)
                        => (await budgetContext.IsEditableAsync(id, cancellationToken))
                                               .GetBudgetEditableStatusError());

                RuleFor(x => x.CategoryId)
                    .MustAsync(async (command, categoryId, cancellationToken)
                        => !await categoryContext.IsAssociatedToBudgetAsync(categoryId, command.BudgetId, cancellationToken))
                        .When(
                            command => command.CategoryId != Guid.Empty,
                            ApplyConditionTo.CurrentValidator)
                            .WithMessage("Category is already associated with Budget.")
                            .WithErrorCode(ErrorCodes.BudgetBudgetCategoryAssociated);
            });
    }
}
