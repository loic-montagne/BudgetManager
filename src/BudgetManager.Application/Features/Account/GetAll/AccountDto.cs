namespace BudgetManager.Application.Features.Account.GetAll;

public sealed record AccountDto(Guid Id, string Name, bool IsClosed, string Iban, string BankName, string Bic);
