using BudgetManager.Infrastructure;
using BudgetManager.Application.Abstractions.Authentication;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Abstractions.Localization;
using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Infrastructure.Persistence.Queries;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Infrastructure.Localization;
using BudgetManager.Infrastructure.Email;
using BudgetManager.Infrastructure.Email.Smtp;
using BudgetManager.Infrastructure.Persistence.Repositories;
using BudgetManager.Infrastructure.Persistence.Seed;
using BudgetManager.Infrastructure.Tests.Fixtures;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using BudgetManager.Infrastructure.Configuration;

namespace BudgetManager.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_WhenServicesIsNull_Throws()
    {
        // Arrange

        IServiceCollection services =
            null!;

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false"
                });

        // Act

        var action = () => services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        // Assert

        Assert.Throws<ArgumentNullException>(
            action);
    }

    [Fact]
    public void AddInfrastructure_WhenConfigurationIsNull_Throws()
    {
        // Arrange

        var services =
            new ServiceCollection();

        // Act

        var action = () => services.AddInfrastructure(
            null!,
            isDevelopment: true);

        // Assert

        Assert.Throws<ArgumentNullException>(
            action);
    }


    [Fact]
    public void AddInfrastructure_WhenConnectionStringIsMissing_Throws()
    {
        // Arrange

        var services =
            new ServiceCollection();

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    [])
                .Build();

        // Act

        var action = () =>
            services.AddInfrastructure(
                configuration,
                isDevelopment: true);

        // Assert

        Assert.Throws<InvalidOperationException>(
            action);
    }

    [Fact]
    public void AddInfrastructure_WhenConfigurationIsValid_RegistersRepositoriesQueriesAndSeeders()
    {
        // Arrange

        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false"
                });

        // Act

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider(
                validateScopes: true);

        using var scope =
            provider.CreateScope();

        // Assert

        Assert.IsType<AccountRepository>(
            scope.ServiceProvider.GetRequiredService<IAccountRepository>());

        Assert.IsType<BankRepository>(
            scope.ServiceProvider.GetRequiredService<IBankRepository>());

        Assert.IsType<BudgetRepository>(
            scope.ServiceProvider.GetRequiredService<IBudgetRepository>());

        Assert.IsType<BudgetCategoryRepository>(
            scope.ServiceProvider.GetRequiredService<IBudgetCategoryRepository>());

        Assert.IsType<TransactionRepository>(
            scope.ServiceProvider.GetRequiredService<ITransactionRepository>());

        Assert.IsType<BudgetManager.Infrastructure.Identity.UserManager>(
            scope.ServiceProvider.GetRequiredService<IUserManager>());

        Assert.IsType<BudgetManager.Infrastructure.Identity.PasswordValidator>(
            scope.ServiceProvider.GetRequiredService<IPasswordValidator>());

        Assert.IsType<BudgetManager.Infrastructure.Identity.ProfilePictureProcessor>(
            scope.ServiceProvider.GetRequiredService<IProfilePictureProcessor>());

        Assert.IsType<PasswordGenerator>(
            scope.ServiceProvider.GetRequiredService<IPasswordGenerator>());

        Assert.IsType<ActivationUrlGenerator>(
            scope.ServiceProvider.GetRequiredService<IActivationUrlGenerator>());

        Assert.IsType<DateTimeLocalizer>(
            scope.ServiceProvider.GetRequiredService<IDateTimeLocalizer>());

        Assert.IsType<EmailTemplateRenderer>(
            scope.ServiceProvider.GetRequiredService<IEmailTemplateRenderer>());

        Assert.IsType<SmtpEmailSender>(
            scope.ServiceProvider.GetRequiredService<IEmailSender>());

        Assert.IsType<TemplatedEmailSender>(
            scope.ServiceProvider.GetRequiredService<ITemplatedEmailSender>());

        Assert.IsType<AccountQueries>(
            scope.ServiceProvider.GetRequiredService<IAccountQueries>());

        Assert.IsType<BudgetQueries>(
            scope.ServiceProvider.GetRequiredService<IBudgetQueries>());

        Assert.Equal(
            2,
            scope.ServiceProvider.GetServices<IDataSeeder>().Count());
    }

    [Fact]
    public void AddInfrastructure_WhenEnabledSeedUserHasInvalidRole_OptionsValidationFails()
    {
        // Arrange

        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "true",
                    ["Seed:Users:Users:Admin:Enabled"] = "true",
                    ["Seed:Users:Users:Admin:Role"] = "Unknown",
                    ["Seed:Users:Users:Admin:Email"] = "admin@example.test",
                    ["Seed:Users:Users:Admin:Password"] = "ValidPassword!123",
                    ["Seed:Users:Users:Admin:LastName"] = "Admin",
                    ["Seed:Users:Users:Admin:FirstName"] = "Bootstrap"
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider(
                validateScopes: true);

        // Act

        var action = () =>
            provider.GetRequiredService<IOptions<UsersSeedOptions>>().Value;

        // Assert

        Assert.Throws<OptionsValidationException>(
            action);
    }

    [Fact]
    public void AddInfrastructure_WhenUserSeedingEnabledWithoutEnabledUser_OptionsValidationFails()
    {
        // Arrange

        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "true",
                    ["Seed:Users:Users:Disabled:Enabled"] = "false"
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider(
                validateScopes: true);

        // Act

        var action = () =>
            provider.GetRequiredService<IOptions<UsersSeedOptions>>().Value;

        // Assert

        Assert.Throws<OptionsValidationException>(
            action);
    }

    [Fact]
    public void AddInfrastructure_WhenSmtpConfigurationIsValid_OptionsAreBound()
    {
        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false",
                    ["Smtp:UserName"] = "smtp-user",
                    ["Smtp:Password"] = "smtp-password"
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider();

        var options =
            provider.GetRequiredService<IOptions<SmtpOptions>>().Value;

        Assert.Equal(
            "smtp.example.test",
            options.Host);

        Assert.Equal(
            587,
            options.Port);

        Assert.Equal(
            SecureSocketOptions.StartTls,
            options.SecureOptions);

        Assert.Equal(
            10000,
            options.Timeout);

        Assert.Equal(
            "smtp-user",
            options.UserName);

        Assert.Equal(
            "smtp-password",
            options.Password);

        Assert.Equal(
            "noreply@example.test",
            options.FromAddress);

        Assert.Equal(
            "Budget Manager",
            options.FromName);
    }

    [Fact]
    public void AddInfrastructure_WhenSmtpAuthenticationIsNotConfigured_OptionsValidationSucceeds()
    {
        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false",
                    ["Smtp:UserName"] = string.Empty,
                    ["Smtp:Password"] = string.Empty
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider();

        var exception =
            Record.Exception(
                () => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);

        Assert.Null(
            exception);
    }

    [Theory]
    [InlineData("Smtp:Host", "")]
    [InlineData("Smtp:Port", "0")]
    [InlineData("Smtp:SecureOptions", "999")]
    [InlineData("Smtp:Timeout", "0")]
    [InlineData("Smtp:FromAddress", "")]
    [InlineData("Smtp:FromName", "")]
    public void AddInfrastructure_WhenSmtpConfigurationValueIsInvalid_OptionsValidationFails(
        string key,
        string value)
    {
        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false",
                    [key] = value
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider();

        var action = () =>
            provider.GetRequiredService<IOptions<SmtpOptions>>().Value;

        Assert.Throws<OptionsValidationException>(
            action);
    }

    [Theory]
    [InlineData("smtp-user", "")]
    [InlineData("", "smtp-password")]
    public void AddInfrastructure_WhenOnlyOneSmtpCredentialIsConfigured_OptionsValidationFails(
        string userName,
        string password)
    {
        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false",
                    ["Smtp:UserName"] = userName,
                    ["Smtp:Password"] = password
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider();

        var action = () =>
            provider.GetRequiredService<IOptions<SmtpOptions>>().Value;

        Assert.Throws<OptionsValidationException>(
            action);
    }

    private static ServiceCollection CreateServices()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddSingleton<ICurrentUser>(
            new TestCurrentUser(
                Guid.NewGuid()));

        services.AddSingleton<TimeProvider>(
            TimeProvider.System);

        return services;
    }

    private static IConfiguration CreateConfiguration(
        IDictionary<string, string?> values)
    {
        var data =
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=localhost;Database=BudgetManagerTests;Trusted_Connection=True;TrustServerCertificate=True",
                ["Smtp:Host"] = "smtp.example.test",
                ["Smtp:Port"] = "587",
                ["Smtp:SecureOptions"] = nameof(SecureSocketOptions.StartTls),
                ["Smtp:Timeout"] = "10000",
                ["Smtp:UserName"] = string.Empty,
                ["Smtp:Password"] = string.Empty,
                ["Smtp:FromAddress"] = "noreply@example.test",
                ["Smtp:FromName"] = "Budget Manager"
            };

        foreach (var value in values)
        {
            data[value.Key] = value.Value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                data)
            .Build();
    }
    [Fact]
    public void AddInfrastructure_ConfiguresIdentityPolicies()
    {
        // Arrange

        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false"
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider(
                validateScopes: true);

        // Act

        var options =
            provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        // Assert

        Assert.Equal(
            12,
            options.Password.RequiredLength);

        Assert.Equal(
            4,
            options.Password.RequiredUniqueChars);

        Assert.True(
            options.Password.RequireDigit);

        Assert.True(
            options.Password.RequireUppercase);

        Assert.True(
            options.Password.RequireLowercase);

        Assert.True(
            options.Password.RequireNonAlphanumeric);

        Assert.True(
            options.User.RequireUniqueEmail);

        Assert.True(
            options.SignIn.RequireConfirmedAccount);

        Assert.True(
            options.SignIn.RequireConfirmedEmail);

        Assert.Equal(
            5,
            options.Lockout.MaxFailedAccessAttempts);

        Assert.Equal(
            TimeSpan.FromMinutes(15),
            options.Lockout.DefaultLockoutTimeSpan);
    }


    [Fact]
    public void AddInfrastructure_ConfiguresDedicatedIdentityTokenProvidersAndLifetimes()
    {
        // Arrange

        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false",
                    ["IdentityTokens:AccountActivationLifetime"] = "7.00:00:00",
                    ["IdentityTokens:EmailChangeLifetime"] = "1.00:00:00",
                    ["IdentityTokens:PasswordResetLifetime"] = "01:00:00"
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider(
                validateScopes: true);

        // Act

        var identityOptions =
            provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        var activationOptions =
            provider.GetRequiredService<IOptions<AccountActivationTokenProviderOptions>>().Value;

        var emailChangeOptions =
            provider.GetRequiredService<IOptions<EmailChangeTokenProviderOptions>>().Value;

        var passwordResetOptions =
            provider.GetRequiredService<IOptions<PasswordResetTokenProviderOptions>>().Value;

        // Assert

        Assert.Equal(
            UserTokenProviders.AccountActivation,
            identityOptions.Tokens.EmailConfirmationTokenProvider);

        Assert.Equal(
            UserTokenProviders.EmailChange,
            identityOptions.Tokens.ChangeEmailTokenProvider);

        Assert.Equal(
            UserTokenProviders.PasswordReset,
            identityOptions.Tokens.PasswordResetTokenProvider);

        Assert.Equal(
            TimeSpan.FromDays(7),
            activationOptions.TokenLifespan);

        Assert.Equal(
            TimeSpan.FromHours(24),
            emailChangeOptions.TokenLifespan);

        Assert.Equal(
            TimeSpan.FromHours(1),
            passwordResetOptions.TokenLifespan);
    }

    [Fact]
    public void AddInfrastructure_WhenIdentityTokenLifetimeIsNotPositive_OptionsValidationFails()
    {
        // Arrange

        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false",
                    ["IdentityTokens:AccountActivationLifetime"] = "00:00:00",
                    ["IdentityTokens:EmailChangeLifetime"] = "1.00:00:00",
                    ["IdentityTokens:PasswordResetLifetime"] = "01:00:00"
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider();

        // Act

        var action = () =>
            provider.GetRequiredService<IOptions<IdentityTokenOptions>>().Value;

        // Assert

        Assert.Throws<OptionsValidationException>(
            action);
    }

    [Fact]
    public void AddInfrastructure_WhenTimeZoneIsInvalid_OptionsValidationFails()
    {
        // Arrange

        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false",
                    ["Localization:TimeZone"] = "Invalid/TimeZone"
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider();

        // Act

        var action = () =>
            provider.GetRequiredService<IOptions<LocalizationOptions>>().Value;

        // Assert

        Assert.Throws<OptionsValidationException>(
            action);
    }

    [Fact]
    public void PasswordGenerator_GeneratesPasswordMatchingConfiguredIdentityPolicy()
    {
        // Arrange

        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false"
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider(
                validateScopes: true);

        using var scope =
            provider.CreateScope();

        var passwordGenerator =
            scope.ServiceProvider.GetRequiredService<IPasswordGenerator>();

        // Act

        var password =
            passwordGenerator.Generate(20);

        // Assert

        Assert.Equal(
            20,
            password.Length);

        Assert.Contains(password, char.IsLower);

        Assert.Contains(password, char.IsUpper);

        Assert.Contains(password, char.IsDigit);

        Assert.Contains(password, character => !char.IsLetterOrDigit(character));

        Assert.True(
            password.Distinct().Count() >= 4);
    }

    [Fact]
    public void PasswordGenerator_WhenRequestedLengthIsBelowPolicy_ThrowsArgumentOutOfRangeException()
    {
        // Arrange

        var services =
            CreateServices();

        var configuration =
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Seed:Users:Enabled"] = "false"
                });

        services.AddInfrastructure(
            configuration,
            isDevelopment: true);

        using var provider =
            services.BuildServiceProvider(
                validateScopes: true);

        using var scope =
            provider.CreateScope();

        var passwordGenerator =
            scope.ServiceProvider.GetRequiredService<IPasswordGenerator>();

        // Act

        var action = () =>
            passwordGenerator.Generate(11);

        // Assert

        Assert.Throws<ArgumentOutOfRangeException>(
            action);
    }


}
