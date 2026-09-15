using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Extensions;
using FluentValidation;
using FluentValidation.Results;

namespace BudgetManager.Application.Features.Budget.DissociateCategory;

public sealed class DissociateCategoryCommandValidator : AbstractValidator<DissociateCategoryCommand>
{
    public DissociateCategoryCommandValidator(IBudgetContext budgetContext, IBudgetCategoryContext budgetCategoryContext, IBudgetCategoryRepository budgetCategoryRepository)
    {
        RuleFor(x => x.CategoryId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("CategoryId is required.")
                .WithErrorCode(ErrorCodes.BudgetBudgetCategoryRequired)
            .MustAsync(budgetCategoryContext.ExistsAsync)
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
            .CustomAsync(async (id, context, cancellationToken)
                        => (await budgetContext.IsEditableAsync(id, cancellationToken))
                                               .GetBudgetEditableStatusError());

        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
            {
                var budget = await budgetContext.GetAsync(
                    command.BudgetId,
                    cancellationToken);

                var category = await budgetCategoryContext.GetAsync(
                    command.CategoryId,
                    cancellationToken);

                if (budget is null || category is null)
                    return;

                if (!await budgetCategoryContext.IsAssociatedToBudgetAsync(
                        command.CategoryId,
                        command.BudgetId,
                        cancellationToken))
                {
                    context.AddFailure(new ValidationFailure(
                        nameof(command.CategoryId),
                        "Category is not associated with budget.")
                    {
                        ErrorCode = ErrorCodes.BudgetBudgetCategoryNotAssociated
                    });
                }

                if (await budgetCategoryRepository.IsUsedAsync(command.CategoryId, command.BudgetId, cancellationToken))
                {
                    context.AddFailure(new ValidationFailure(
                        nameof(command.CategoryId),
                        "Category is used in this budget.")
                    {
                        ErrorCode = ErrorCodes.BudgetBudgetCategoryIsUsedInBudget
                    });
                }
            });
    }
}
