using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ConfigureAuditableEntity();
        
        builder.Ignore(x => x.SignedAmount);

        builder.Property(x => x.Name)
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(Domain.Common.StringPropertyLengths.NameLength)
               .IsRequired();
        builder.Property(x => x.Type)
               .IsRequired();
        builder.Property(x => x.Amount)
               .IsRequired();
        builder.Property(x => x.Method)
               .IsRequired();
        builder.Property(x => x.BudgetId)
               .IsRequired();
        builder.Property(x => x.CategoryId)
               .IsRequired();
        builder.Property(x => x.AccountId)
               .IsRequired();

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => new { x.BudgetId, x.CategoryId, x.Name })
               .IsUnique();

        builder.HasOne(x => x.Budget)
               .WithMany(x => x.Transactions)
               .HasForeignKey(x => x.BudgetId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
               .WithMany()
               .HasForeignKey(x => x.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Account)
               .WithMany()
               .HasForeignKey(x => x.AccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TransferAccount)
               .WithMany()
               .HasForeignKey(x => x.TransferAccountId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
