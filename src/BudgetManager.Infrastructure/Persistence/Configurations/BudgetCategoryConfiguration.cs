using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configurations;

internal sealed class BudgetCategoryConfiguration : IEntityTypeConfiguration<BudgetCategory>
{
    public void Configure(EntityTypeBuilder<BudgetCategory> builder)
    {
        builder.ConfigureAuditableEntity();
        builder.ConfigureOptimisticConcurrencyToken();

        builder.Property(x => x.Name)
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(Domain.Common.StringPropertyLengths.NameLength)
               .IsRequired();
        builder.Property(x => x.Description)
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(Domain.Common.StringPropertyLengths.DescriptionLength);

        builder.HasIndex(x => x.Name)
               .IsUnique();

        builder.HasMany(x => x.Budgets)
               .WithMany(x => x.Categories);
    }
}
