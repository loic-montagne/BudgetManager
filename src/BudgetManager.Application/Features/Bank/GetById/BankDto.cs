namespace BudgetManager.Application.Features.Bank.GetById;

public sealed record BankDto(Guid Id, string Name, string Bic, int AccountsCount, Guid CreatedBy, string CreatedByName, DateTimeOffset CreatedOn, Guid UpdatedBy, string UpdatedByName, DateTimeOffset UpdatedOn);
