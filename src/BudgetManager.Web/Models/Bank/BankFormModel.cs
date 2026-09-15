using BudgetManager.Web.Models.Common;

namespace BudgetManager.Web.Models.Bank;

public sealed class BankFormModel : AuditableFormModel
{
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Bic { get; set; } = string.Empty;
}
