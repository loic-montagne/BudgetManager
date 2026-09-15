namespace BudgetManager.Application.Features.User.Common;

public sealed record UserDto(Guid Id, string UserName, string LastName, string FirstName);
