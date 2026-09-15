using BudgetManager.Application.Abstractions.Authentication;

namespace BudgetManager.Infrastructure.Tests.Fixtures;

internal sealed class TestCurrentUser(Guid? userId = null) : ICurrentUser
{
    private readonly HashSet<string> _roles = [];

    public bool IsAuthenticated => UserId.HasValue;
    public Guid? UserId { get; set; } = userId;
    public string? UserName { get; set; } = "test-user";
    public IEnumerable<string> RolesNames => _roles;

    public bool IsInRole(string role) => _roles.Contains(role);
    public string? GetClaim(string type) => null;
    public bool HasClaim(string type) => false;
}
