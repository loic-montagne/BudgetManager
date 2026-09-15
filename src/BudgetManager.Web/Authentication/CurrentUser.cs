using BudgetManager.Application.Abstractions.Authentication;
using System.Security.Claims;

namespace BudgetManager.Web.Authentication;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor?.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
    public string? UserName => User?.Identity?.Name;

    public IEnumerable<string> RolesNames => User?.FindAll(ClaimTypes.Role)
                                                  .Select(c => c.Value)
                                                  .Distinct() ?? Enumerable.Empty<string>();

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
    public string? GetClaim(string type) => User?.FindFirst(type)?.Value;
    public bool HasClaim(string type) => User?.HasClaim(c => c.Type == type) ?? false;
}
