using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configurations;

internal sealed class BudgetCategoryAssociationConfiguration : IEntityTypeConfiguration<BudgetCategoryAssociation>
{
    public void Configure(EntityTypeBuilder<BudgetCategoryAssociation> builder)
    {
        builder.HasKey(x => new { x.BudgetId, x.CategoryId });

        builder.Property(x => x.BudgetId)
               .IsRequired();
        builder.Property(x => x.CategoryId)
               .IsRequired();
        builder.Property(x => x.Order)
               .IsRequired();

        builder.HasOne(x => x.Budget)
               .WithMany(x => x.AssociatedCategories)
               .HasForeignKey(x => x.BudgetId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
               .WithMany(x => x.AssociatedBudgets)
               .HasForeignKey(x => x.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
