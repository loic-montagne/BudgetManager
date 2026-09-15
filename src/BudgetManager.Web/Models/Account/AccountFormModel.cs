using BudgetManager.Web.Models.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BudgetManager.Web.Models.Account;

public sealed class AccountFormModel : AuditableFormModel
{
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Iban { get; set; } = string.Empty;

    public Guid BankId { get; set; } = Guid.Empty;

    public List<SelectListItem> Banks { get; set; } = [];
}
