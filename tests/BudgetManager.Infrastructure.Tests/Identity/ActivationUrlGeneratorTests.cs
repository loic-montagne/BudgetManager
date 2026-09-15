using BudgetManager.Application.Abstractions.Api;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Infrastructure.Persistence;
using BudgetManager.Infrastructure.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Identity;

[Collection(SqlServerCollection.Name)]
public sealed class ActivationUrlGeneratorTests(SqlServerFixture fixture) : InfrastructureTestBase(fixture)
{
    [Fact]
    public async Task Generate_WhenUserExists_PreservesRouteValuesAndAddsUsableActivationCode()
    {
        // Arrange

        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await CreateUserAsync(identity);
        var builder = scope.ServiceProvider.GetRequiredService<CapturingApplicationUrlBuilder>();
        var generator = scope.ServiceProvider.GetRequiredService<IActivationUrlGenerator>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act

        var result = await generator.Generate(
            user.Id,
            "/Account/ActivateAccount",
            new { area = "Identity", returnUrl = "/" },
            cancellationToken);

        // Assert

        Assert.Equal("https://example.test/generated", result);
        Assert.Equal("/Account/ActivateAccount", builder.PageName);
        Assert.NotNull(builder.Values);
        Assert.Equal("Identity", builder.Values["area"]);
        Assert.Equal("/", builder.Values["returnUrl"]);

        var activationCode = Assert.IsType<string>(builder.Values["activationCode"]);
        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(activationCode));
        var confirmationResult = await identity.ConfirmEmailAsync(user, token);

        Assert.True(
            confirmationResult.Succeeded,
            string.Join("; ", confirmationResult.Errors.Select(x => x.Description)));
    }

    [Fact]
    public async Task Generate_WhenRouteValuesContainActivationCode_ReplacesItWithGeneratedCode()
    {
        // Arrange

        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await CreateUserAsync(identity);
        var builder = scope.ServiceProvider.GetRequiredService<CapturingApplicationUrlBuilder>();
        var generator = scope.ServiceProvider.GetRequiredService<IActivationUrlGenerator>();

        // Act

        await generator.Generate(
            user.Id,
            "/Account/ActivateAccount",
            new { activationCode = "caller-value" },
            TestContext.Current.CancellationToken);

        // Assert

        Assert.NotNull(builder.Values);
        var activationCode = Assert.IsType<string>(builder.Values["activationCode"]);
        Assert.NotEqual("caller-value", activationCode);
    }

    [Fact]
    public async Task Generate_WhenUserDoesNotExist_ThrowsNullReferenceException()
    {
        // Arrange

        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var generator = scope.ServiceProvider.GetRequiredService<IActivationUrlGenerator>();

        // Act

        var action = () => generator.Generate(
            Guid.NewGuid(),
            "/Account/ActivateAccount",
            new { area = "Identity" },
            TestContext.Current.CancellationToken);

        // Assert

        await Assert.ThrowsAsync<NullReferenceException>(action);
    }

    private ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(DatabaseConnectionString));
        services
            .AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IActivationUrlGenerator, ActivationUrlGenerator>();
        services.AddScoped<CapturingApplicationUrlBuilder>();
        services.AddScoped<IApplicationUrlBuilder>(sp =>
            sp.GetRequiredService<CapturingApplicationUrlBuilder>());

        return services.BuildServiceProvider();
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> manager)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "activation-url@example.test",
            Email = "activation-url@example.test",
            LastName = "Last",
            FirstName = "First"
        };

        var result = await manager.CreateAsync(user, "ValidPassword!123");

        Assert.True(
            result.Succeeded,
            string.Join("; ", result.Errors.Select(x => x.Description)));

        return user;
    }

    private sealed class CapturingApplicationUrlBuilder : IApplicationUrlBuilder
    {
        public string? PageName { get; private set; }
        public RouteValueDictionary? Values { get; private set; }

        public string GetPageUrl(string pageName, object? values = null)
        {
            PageName = pageName;
            Values = values as RouteValueDictionary ?? new RouteValueDictionary(values);
            return "https://example.test/generated";
        }
    }
}
