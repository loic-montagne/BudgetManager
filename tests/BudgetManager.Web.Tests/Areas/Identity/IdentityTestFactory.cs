using BudgetManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace BudgetManager.Web.Tests.Areas.Identity;

internal static class IdentityTestFactory
{
    internal static ApplicationUser User(string email = "user@example.test")
        => new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = "Jane",
            LastName = "Doe",
            PreferredCulture = "fr-FR"
        };

    internal static UserManager<ApplicationUser> UserManager()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        return Substitute.For<UserManager<ApplicationUser>>(
            store,
            Options.Create(new IdentityOptions()),
            Substitute.For<IPasswordHasher<ApplicationUser>>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            Substitute.For<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<UserManager<ApplicationUser>>>());
    }

    internal static SignInManager<ApplicationUser> SignInManager(UserManager<ApplicationUser>? manager = null)
        => Substitute.For<SignInManager<ApplicationUser>>(
            manager ?? UserManager(),
            Substitute.For<IHttpContextAccessor>(),
            Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Options.Create(new IdentityOptions()),
            Substitute.For<ILogger<SignInManager<ApplicationUser>>>(),
            Substitute.For<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>(),
            Substitute.For<IUserConfirmation<ApplicationUser>>());

    internal static IStringLocalizer<SharedResource> Localizer()
    {
        var localizer = Substitute.For<IStringLocalizer<SharedResource>>();

        localizer[Arg.Any<string>()]
            .Returns(call =>
            {
                var name = call.ArgAt<string>(0);
                return new LocalizedString(name, name);
            });

        localizer[Arg.Any<string>(), Arg.Any<object[]>()]
            .Returns(call =>
            {
                var name = call.ArgAt<string>(0);
                return new LocalizedString(name, name);
            });

        return localizer;
    }

    internal static T Attach<T>(T model) where T : PageModel
    {
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestAborted = TestContext.Current.CancellationToken
            }
        };
        return model;
    }
}
