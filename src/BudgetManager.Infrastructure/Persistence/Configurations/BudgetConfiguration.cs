using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configurations;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ConfigureAuditableEntity();
        builder.ConfigureOptimisticConcurrencyToken();

        builder.Ignore(x => x.Expenses);
        builder.Ignore(x => x.Incomes);
        builder.Ignore(x => x.Balance);

        builder.Property(x => x.Name)
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(Domain.Common.StringPropertyLengths.NameLength)
               .IsRequired();
        builder.Property(x => x.IsLocked)
               .IsRequired();

        builder.HasIndex(x => x.Name)
               .IsUnique();
    }
}
