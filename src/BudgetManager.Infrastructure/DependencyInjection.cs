using BudgetManager.Application.Abstractions.Api;
using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Abstractions.Localization;
using BudgetManager.Application.Abstractions.Persistence;
using BudgetManager.Application.Common;
using BudgetManager.Domain.Extensions;
using BudgetManager.Infrastructure.Api;
using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Infrastructure.Email;
using BudgetManager.Infrastructure.Email.Smtp;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Infrastructure.Localization;
using BudgetManager.Infrastructure.Persistence;
using BudgetManager.Infrastructure.Persistence.Interceptors;
using BudgetManager.Infrastructure.Persistence.Queries;
using BudgetManager.Infrastructure.Persistence.Repositories;
using BudgetManager.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BudgetManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException($"Connection string DefaultConnection not found.");
        var commandTimeOut = configuration.GetValue<int>("Database:CommandTimeout", 120);
        var accountActivationLifetime = configuration.GetValue<TimeSpan>($"{IdentityTokenOptions.SectionName}:{nameof(IdentityTokenOptions.AccountActivationLifetime)}", TimeSpan.FromDays(7));
        var emailChangeLifetime = configuration.GetValue<TimeSpan>($"{IdentityTokenOptions.SectionName}:{nameof(IdentityTokenOptions.EmailChangeLifetime)}", TimeSpan.FromHours(24));
        var passwordResetLifetime = configuration.GetValue<TimeSpan>($"{IdentityTokenOptions.SectionName}:{nameof(IdentityTokenOptions.PasswordResetLifetime)}", TimeSpan.FromHours(1));

        // Options UserSeed
        services
            .AddOptions<UsersSeedOptions>()
            .Bind(configuration.GetSection(UsersSeedOptions.SectionName))
            .Validate(
                options =>
                    !options.Enabled || options.Users.Any(x => x.Value.Enabled),
                "At least one seed user must be configured and enabled when user seeding is enabled.")
            .Validate(
                options =>
                    !options.Enabled || options.Users.Values.All(
                        user =>
                            !user.Enabled ||
                            !string.IsNullOrWhiteSpace(user.Role) &&
                            ApplicationRoles.All.Contains(user.Role) &&
                            !string.IsNullOrWhiteSpace(user.Email) &&
                            !string.IsNullOrWhiteSpace(user.Password) &&
                            !string.IsNullOrWhiteSpace(user.LastName) &&
                            !string.IsNullOrWhiteSpace(user.FirstName)),
                "All enabled seed users must define a valid role, email, password, last name and first name.")
            .ValidateOnStart();

        // Options Email
        services
            .AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Host)
                 && options.Port > 0
                 && options.SecureOptions.IsValid()
                 && options.Timeout > 0
                 && string.IsNullOrWhiteSpace(options.UserName) == string.IsNullOrWhiteSpace(options.Password)
                 && !string.IsNullOrWhiteSpace(options.FromAddress)
                 && !string.IsNullOrWhiteSpace(options.FromName),
                "Email configuration is invalid.")
            .ValidateOnStart();

        // Options IdentityToken
        services
            .AddOptions<IdentityTokenOptions>()
            .Bind(configuration.GetSection(IdentityTokenOptions.SectionName))
            .Validate(
                options =>
                    options.AccountActivationLifetime > TimeSpan.Zero &&
                    options.EmailChangeLifetime > TimeSpan.Zero &&
                    options.PasswordResetLifetime > TimeSpan.Zero,
                "Identity token lifetime configuration is invalid.")
            .ValidateOnStart();

        // Options Localization
        services
            .AddOptions<LocalizationOptions>()
            .Bind(configuration.GetSection(LocalizationOptions.SectionName))
            .Validate(
                options =>
                {
                    try
                    {
                        TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
                        return true;
                    }
                    catch (TimeZoneNotFoundException)
                    {
                        return false;
                    }
                    catch (InvalidTimeZoneException)
                    {
                        return false;
                    }
                },
                "The configured time zone is invalid.")
            .ValidateOnStart();

        // Localization
        services.AddSingleton<IDateTimeLocalizer, DateTimeLocalizer>();

        // Email
        services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        services.AddTransient<ITemplatedEmailSender, TemplatedEmailSender>();

        // Api url builder
        if (isDevelopment)
        {
            services.AddScoped<IApplicationUrlBuilder, DevApplicationUrlBuilder>();
        }
        else
        {
            services
                .AddOptions<ApplicationOptions>()
                .BindConfiguration(ApplicationOptions.SectionName)
                .Validate(
                    options =>
                        options.PublicUrl is
                        {
                            IsAbsoluteUri: true
                        },
                    "Application:PublicUrl must be an absolute URI.")
                .Validate(
                    options =>
                        options.PublicUrl.Scheme == Uri.UriSchemeHttps,
                    "Application:PublicUrl must use HTTPS.")
                .ValidateOnStart();

            services.AddSingleton<IApplicationUrlBuilder, ApplicationUrlBuilder>();
        }

        // Interceptors
        services.AddScoped<IInterceptor, AuditableInterceptor>();

        // DbContext
        services
            .AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    sql.CommandTimeout(commandTimeOut);
                });
            });

        // Identity
        services.AddScoped<IProfilePictureProcessor, ProfilePictureProcessor>();
        services.AddScoped<IPasswordValidator, PasswordValidator>();
        services.AddScoped<IPasswordGenerator, PasswordGenerator>();
        services.AddScoped<IActivationUrlGenerator, ActivationUrlGenerator>();
        services.AddScoped<IUserManager, UserManager>();
        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Configuration des règles pour le mot de passe
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 4;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // Configuration des règles de blocage du compte
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                // Configuration des règles pour l'utilisateur
                options.User.RequireUniqueEmail = true;

                // Configuration des règles pour la connexion
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;

                // Configuration de la durée de vie des tokens
                options.Tokens.EmailConfirmationTokenProvider =
                    UserTokenProviders.AccountActivation;
                options.Tokens.ChangeEmailTokenProvider =
                    UserTokenProviders.EmailChange;
                options.Tokens.PasswordResetTokenProvider =
                    UserTokenProviders.PasswordReset;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<AccountActivationTokenProvider>(UserTokenProviders.AccountActivation)
            .AddTokenProvider<EmailChangeTokenProvider>(UserTokenProviders.EmailChange)
            .AddTokenProvider<PasswordResetTokenProvider>(UserTokenProviders.PasswordReset)
            .AddDefaultUI();
        services.Configure<AccountActivationTokenProviderOptions>(options =>
        {
            options.TokenLifespan = accountActivationLifetime;
        });
        services.Configure<EmailChangeTokenProviderOptions>(options =>
        {
            options.TokenLifespan = emailChangeLifetime;
        });
        services.Configure<PasswordResetTokenProviderOptions>(options =>
        {
            options.TokenLifespan = passwordResetLifetime;
        });

        // Queries
        services.AddScoped<IAccountQueries, AccountQueries>();
        services.AddScoped<IBankQueries, BankQueries>();
        services.AddScoped<IBudgetAccessQueries, BudgetAccessQueries>();
        services.AddScoped<IBudgetCategoryQueries, BudgetCategoryQueries>();
        services.AddScoped<IBudgetQueries, BudgetQueries>();
        services.AddScoped<ITransactionQueries, TransactionQueries>();
        services.AddScoped<IUserQueries, UserQueries>();

        // Repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IBankRepository, BankRepository>();
        services.AddScoped<IBudgetCategoryRepository, BudgetCategoryRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        // Seeders
        services.AddScoped<IDataSeeder, RoleSeeder>();
        services.AddScoped<IDataSeeder, UsersSeeder>();

        // Database initialization
        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    public static async Task UseDatabaseInitializationAsync(this IServiceScope scope, CancellationToken cancellationToken)
    {
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseInitializer>>();

        try
        {
            await initializer.InitializeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database initialization.");
            throw;
        }
    }
}
