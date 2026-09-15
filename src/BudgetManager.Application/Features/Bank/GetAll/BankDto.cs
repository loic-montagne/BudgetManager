namespace BudgetManager.Application.Features.Bank.GetAll;

public sealed record BankDto(Guid Id, string Name, string Bic, int AccountsCount);
