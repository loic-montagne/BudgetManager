using BudgetManager.Application.Features.User.Common;
using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Features.Budget.GetById;

public sealed record AccessDto(UserDto User, bool IsOwner, IReadOnlyList<Permission> Permissions);
public sealed record BudgetCategoryDto(Guid Id, string Name, string? Description, decimal Expenses, decimal Incomes, decimal Balance);
public sealed record TransactionDto(Guid Id, string Name, BudgetCategoryDto Category, TransactionType Type, decimal Amount, decimal SignedAmount, PaymentMethod Method);
public sealed record BudgetDto(Guid Id, string Name, bool IsLocked, decimal Expenses, decimal Incomes, decimal Balance, IReadOnlyList<AccessDto> Accesses, IReadOnlyList<BudgetCategoryDto> Categories, IReadOnlyList<TransactionDto> Transactions, bool IsCurrentUserOwner, IReadOnlyList<Permission> CurrentUserPermissions, Guid CreatedBy, string CreatedByName, DateTimeOffset CreatedOn, Guid UpdatedBy, string UpdatedByName, DateTimeOffset UpdatedOn);
