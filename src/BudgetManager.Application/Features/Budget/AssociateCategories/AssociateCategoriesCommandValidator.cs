using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Extensions;
using FluentValidation;
using FluentValidation.Results;

namespace BudgetManager.Application.Features.Budget.AssociateCategories;

public sealed class AssociateCategoriesCommandValidator : AbstractValidator<AssociateCategoriesCommand>
{
    public AssociateCategoriesCommandValidator(IBudgetContext budgetContext, IBudgetCategoryContext categoryContext)
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
             .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("CategoriesIds must be unique.")
                .WithErrorCode(ErrorCodes.BudgetBudgetCategoryMustBeUnique)
            .ForEach(categoryId =>
            {
                categoryId
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                        .WithMessage("CategoryId is required.")
                        .WithErrorCode(ErrorCodes.BudgetBudgetCategoryRequired)
                    .MustAsync(categoryContext.ExistsAsync)
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
                    var category = await categoryContext.GetAsync(categoryId, cancellationToken);
                    if (category is null)
                        continue;

                    if (await categoryContext.IsAssociatedToBudgetAsync(
                        categoryId,
                        command.BudgetId,
                        cancellationToken))
                    {
                        context.AddFailure(new ValidationFailure(
                            "CategoriesIds",
                            "Category is already associated with budget.")
                        {
                            ErrorCode = ErrorCodes.BudgetBudgetCategoryAssociated
                        });
                    }
                }
            });
    }
}
