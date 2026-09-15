using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Features.Transaction.GetById;

public sealed record BudgetDto(Guid Id, string Name);
public sealed record BudgetCategoryDto(Guid Id, string Name, string? Description);
public sealed record AccountDto(Guid Id, string Name, bool IsClosed, string Iban, string BankName, string Bic);
public sealed record TransactionDto(Guid Id, string Name, TransactionType Type, decimal Amount, decimal SignedAmount, PaymentMethod Method, BudgetDto Budget, BudgetCategoryDto Category, AccountDto Account, AccountDto? TransferAccount, Guid CreatedBy, string CreatedByName, DateTimeOffset CreatedOn, Guid UpdatedBy, string UpdatedByName, DateTimeOffset UpdatedOn);
