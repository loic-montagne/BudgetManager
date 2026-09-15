using BudgetManager.Application.Common.Errors;

namespace BudgetManager.Web.Services;

public interface IBusinessErrorLocalizer
{
    string Localize(ValidationError error);
}
