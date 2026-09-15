using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Exceptions;

namespace BudgetManager.Application.Tests;

internal sealed class TestCurrentUser : ICurrentUser
{
    private readonly HashSet<string> _roles;

    public TestCurrentUser(
        bool authenticated = true,
        Guid? userId = null,
        params string[] roles)
    {
        IsAuthenticated = authenticated;
        UserId = userId ?? Guid.NewGuid();
        UserName = "test-user";
        _roles = roles.ToHashSet(StringComparer.Ordinal);
    }

    public bool IsAuthenticated { get; }

    public Guid? UserId { get; set; }

    public string? UserName { get; }

    public IEnumerable<string> RolesNames => _roles;

    public Guid RequiredUserId => UserId ?? throw new UnauthenticatedException(this);

    public bool IsInRole(string role)
    {
        return _roles.Contains(role);
    }

    public string? GetClaim(string type)
    {
        return null;
    }

    public bool HasClaim(string type)
    {
        return false;
    }
}
