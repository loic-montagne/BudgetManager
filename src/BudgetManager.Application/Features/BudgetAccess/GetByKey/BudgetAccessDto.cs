using BudgetManager.Application.Features.User.Common;
using BudgetManager.Domain.Enums;

namespace BudgetManager.Application.Features.BudgetAccess.GetByKey;

public sealed record BudgetDto(Guid Id, string Name);
public sealed record BudgetAccessDto(BudgetDto Budget, UserDto User, bool IsOwner, IReadOnlyList<Permission> Permissions, Guid CreatedBy, string CreatedByName, DateTimeOffset CreatedOn, Guid UpdatedBy, string UpdatedByName, DateTimeOffset UpdatedOn);
