using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Extensions;
using FluentValidation;
using FluentValidation.Results;

namespace BudgetManager.Application.Features.Budget.ReorderCategories;

public sealed class ReorderCategoriesCommandValidator : AbstractValidator<ReorderCategoriesCommand>
{
    public ReorderCategoriesCommandValidator(IBudgetContext budgetContext, IBudgetCategoryContext budgetCategoryContext)
    {
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

        RuleFor(x => x.CategoriesIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("CategoriesIds is required.")
                .WithErrorCode(ErrorCodes.BudgetBudgetCategoryRequired)
            .ForEach(categoryId =>
            {
                categoryId
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                        .WithMessage("CategoryId is required.")
                        .WithErrorCode(ErrorCodes.BudgetBudgetCategoryRequired)
                    .MustAsync(budgetCategoryContext.ExistsAsync)
                        .WithMessage("CategoryId must exist.")
                        .WithErrorCode(ErrorCodes.BudgetBudgetCategoryNotExists);
            });

        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
            {
                var budget = await budgetContext.GetAsync(
                    command.BudgetId,
                    cancellationToken);

                if (budget is null || command.CategoriesIds is null || command.CategoriesIds.Count == 0)
                    return;

                foreach (var categoryId in command.CategoriesIds)
                {
                    var category = await budgetCategoryContext.GetAsync(categoryId, cancellationToken);
                    if (category is null)
                        continue;

                    if (!await budgetCategoryContext.IsAssociatedToBudgetAsync(
                        categoryId,
                        command.BudgetId,
                        cancellationToken))
                    {
                        context.AddFailure(new ValidationFailure(
                            "CategoryId",
                            "Category is not associated with budget.")
                        {
                            ErrorCode = ErrorCodes.BudgetBudgetCategoryNotAssociated
                        });
                    }
                }
            });
    }
}
