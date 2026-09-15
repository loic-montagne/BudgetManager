namespace BudgetManager.Application.Features.Bank.Search;

public sealed record BankDto(Guid Id, string Name, string Bic, int AccountsCount);
