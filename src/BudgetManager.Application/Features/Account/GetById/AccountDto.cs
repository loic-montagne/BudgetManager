namespace BudgetManager.Application.Features.Account.GetById;

public sealed record BankDto(Guid Id, string Name, string Bic);
public sealed record AccountDto(Guid Id, string Name, bool IsClosed, string Iban, BankDto Bank, Guid CreatedBy, string CreatedByName, DateTimeOffset CreatedOn, Guid UpdatedBy, string UpdatedByName, DateTimeOffset UpdatedOn);
