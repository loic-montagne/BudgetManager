using BudgetManager.Domain.Entities.Common;
using BudgetManager.Domain.Entities.Interfaces;
using BudgetManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configurations;

internal static class EntityTypeBuilderExtensions
{
    public static void ConfigureAuditable<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable
    {
        builder.Property(x => x.CreatedOn)
               .IsRequired();
        builder.Property(x => x.CreatedBy)
               .IsRequired();
        builder.Property(x => x.UpdatedOn)
               .IsRequired();
        builder.Property(x => x.UpdatedBy)
               .IsRequired();
    }

    public static void ConfigureEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IEntity
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
               .ValueGeneratedNever();
    }

    public static void ConfigureAuditableEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IEntity, IAuditable
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
    }

    public static void ConfigureOptimisticConcurrencyToken<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IHasOptimisticConcurrencyToken
    {
        builder.Property(x => x.RowVersion)
               .IsRowVersion();
    }
}
