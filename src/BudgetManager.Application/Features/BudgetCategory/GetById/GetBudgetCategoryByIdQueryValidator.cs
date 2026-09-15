using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Common.Errors;
using FluentValidation;

namespace BudgetManager.Application.Features.BudgetCategory.GetById;

public sealed class GetBudgetCategoryByIdQueryValidator : AbstractValidator<GetBudgetCategoryByIdQuery>
{
    public GetBudgetCategoryByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
                .WithMessage("Id is required.")
                .WithErrorCode(ErrorCodes.BudgetCategoryIdRequired);
    }
}
