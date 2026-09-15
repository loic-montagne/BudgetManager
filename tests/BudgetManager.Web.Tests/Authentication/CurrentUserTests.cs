using BudgetManager.Web.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;

namespace BudgetManager.Web.Tests.Authentication;

public sealed class CurrentUserTests
{
    [Fact]
    public void CurrentUser_WithAuthenticatedPrincipal_ExposesIdentityClaimsAndDistinctRoles()
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, "alice"),
            new Claim(ClaimTypes.Role, "Administrator"),
            new Claim(ClaimTypes.Role, "Administrator"),
            new Claim("custom", "value")], "test"));
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } };
        var currentUser = new CurrentUser(accessor);
        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(id, currentUser.UserId);
        Assert.Equal("alice", currentUser.UserName);
        Assert.Equal(["Administrator"], currentUser.RolesNames);
        Assert.True(currentUser.IsInRole("Administrator"));
        Assert.Equal("value", currentUser.GetClaim("custom"));
        Assert.True(currentUser.HasClaim("custom"));
    }

    [Fact]
    public void CurrentUser_WithoutHttpContext_ReturnsAnonymousDefaults()
    {
        var currentUser = new CurrentUser(new HttpContextAccessor());
        Assert.False(currentUser.IsAuthenticated);
        Assert.Null(currentUser.UserId);
        Assert.Null(currentUser.UserName);
        Assert.Empty(currentUser.RolesNames);
        Assert.False(currentUser.IsInRole("Administrator"));
        Assert.Null(currentUser.GetClaim("custom"));
        Assert.False(currentUser.HasClaim("custom"));
    }
}
