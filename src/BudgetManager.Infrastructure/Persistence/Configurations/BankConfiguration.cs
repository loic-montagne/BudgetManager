using BudgetManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configurations;

internal sealed class BankConfiguration : IEntityTypeConfiguration<Bank>
{
    public void Configure(EntityTypeBuilder<Bank> builder)
    {
        builder.ConfigureAuditableEntity();
        builder.ConfigureOptimisticConcurrencyToken();

        builder.Property(x => x.Name)
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(Domain.Common.StringPropertyLengths.NameLength)
               .IsRequired();

        builder.ComplexProperty(
            x => x.Bic,
            bic =>
            {
                bic.Property(value => value.Value)
                    .HasColumnName("Bic")
                    .UseCollation(ApplicationDbContext.StringComparisonCollation)
                    .HasMaxLength(11)
                    .IsRequired();
            });

        builder.HasIndex(x => x.Name)
               .IsUnique();

        // TODO : Décommenter avec EFCore 11
        //builder.HasIndex(x => x.Bic.Value)
        //       .IsUnique();
        // En attendant : ajouter dans la première migration :
        // migrationBuilder.CreateIndex(
        //     name: "IX_Banks_Bic",
        //     table: "Banks",
        //     column: "Bic",
        //     unique: true);
    }
}
