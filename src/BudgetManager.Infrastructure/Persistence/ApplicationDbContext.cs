using BudgetManager.Application.Exceptions;
using BudgetManager.Domain.Entities;
using BudgetManager.Domain.ValueObjects;
using BudgetManager.Infrastructure.Extensions;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BudgetManager.Infrastructure.Persistence;

internal sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IEnumerable<IInterceptor> interceptors,
    ILogger<ApplicationDbContext> logger)
    : IdentityDbContext<
        ApplicationUser, 
        ApplicationRole, 
        Guid, 
        IdentityUserClaim<Guid>, 
        ApplicationUserRole, 
        IdentityUserLogin<Guid>, 
        IdentityRoleClaim<Guid>, 
        IdentityUserToken<Guid>>(options)
{
    public const string StringComparisonCollation = "SQL_Latin1_General_CP1_CI_AI";

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetAccess> BudgetAccesses => Set<BudgetAccess>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<ApplicationUserProfilePicture> UserProfilePictures => Set<ApplicationUserProfilePicture>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<UDecimal>()
               .HaveConversion<UDecimalConverter>()
               .HavePrecision(18, 2);

        base.ConfigureConventions(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (interceptors is not null)
            optionsBuilder.AddInterceptors(interceptors);
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }


    public override int SaveChanges()
    {
        try
        {
            return base.SaveChanges();
        }
        catch(DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while saving changes :\r\n{errors}", ex.ToErrorString());
            throw new ConcurrencyException("The entity was modified by another operation.", ex);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error while saving changes :\r\n{errors}", ex.ToErrorString());
            throw new UpdateException("The entity was not saved.", ex);
        }
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while saving changes :\r\n{errors}", ex.ToErrorString());
            throw new ConcurrencyException("The entity was modified by another operation.", ex);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error while saving changes :\r\n{errors}", ex.ToErrorString());
            throw new UpdateException("The entity was not saved.", ex);
        }
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while saving changes :\r\n{errors}", ex.ToErrorString());
            throw new ConcurrencyException("The entity was modified by another operation.", ex);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error while saving changes :\r\n{errors}", ex.ToErrorString());
            throw new UpdateException("The entity was not saved.", ex);
        }
    }
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while saving changes :\r\n{errors}", ex.ToErrorString());
            throw new ConcurrencyException("The entity was modified by another operation.", ex);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error while saving changes :\r\n{errors}", ex.ToErrorString());
            throw new UpdateException("The entity was not saved.", ex);
        }
    }
}
