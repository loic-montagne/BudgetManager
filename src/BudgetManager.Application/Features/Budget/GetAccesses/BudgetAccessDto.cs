using BudgetManager.Application.Features.User.Common;
using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Features.Budget.GetAccesses;

public sealed record BudgetDto(Guid Id, string Name);
public sealed record BudgetAccessDto(BudgetDto Budget, UserDto User, bool IsOwner, IReadOnlyList<Permission> Permissions);
