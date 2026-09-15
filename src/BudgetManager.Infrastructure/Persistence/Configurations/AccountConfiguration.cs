using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ConfigureAuditableEntity();
        builder.ConfigureOptimisticConcurrencyToken();

        builder.Property(x => x.Name)
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(Domain.Common.StringPropertyLengths.NameLength)
               .IsRequired();
        builder.Property(x => x.IsClosed)
               .IsRequired();
        builder.Property(x => x.BankId)
               .IsRequired();

        builder.ComplexProperty(
            x => x.Iban,
            iban =>
            {
                iban.Property(value => value.Value)
                    .HasColumnName("Iban")
                    .UseCollation(ApplicationDbContext.StringComparisonCollation)
                    .HasMaxLength(34)
                    .IsRequired();
            });

        builder.HasIndex(x => x.Name)
               .IsUnique();

        // TODO : Décommenter avec EFCore 11
        //builder.HasIndex(x => x.Iban.Value)
        //       .IsUnique();
        // En attendant : ajouter dans la première migration :
        // migrationBuilder.CreateIndex(
        //     name: "IX_Accounts_Iban",
        //     table: "Accounts",
        //     column: "Iban",
        //     unique: true);

        builder.HasOne(x => x.Bank)
               .WithMany(x => x.Accounts)
               .HasForeignKey(x => x.BankId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
