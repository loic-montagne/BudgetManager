using BudgetManager.Application.Abstractions.Contexts;
using BudgetManager.Application.Behaviors;
using BudgetManager.Application.Contexts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = typeof(DependencyInjection).Assembly;

        services.AddScoped<EntityCacheContext>();
        services.AddScoped<IAccountContext, AccountContext>();
        services.AddScoped<IBankContext, BankContext>();
        services.AddScoped<IBudgetContext, BudgetContext>();
        services.AddScoped<IBudgetCategoryContext, BudgetCategoryContext>();
        services.AddScoped<ITransactionContext, TransactionContext>();
        services.AddScoped<IUserContext, UserContext>();

        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Transient);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
