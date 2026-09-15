using BudgetManager.Domain.Entities;
using BudgetManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configurations;

internal sealed class BudgetAccessConfiguration : IEntityTypeConfiguration<BudgetAccess>
{
    public void Configure(EntityTypeBuilder<BudgetAccess> builder)
    {
        builder.ConfigureAuditable();

        builder.HasKey(x => new { x.UserId, x.BudgetId });

        builder.Property(x => x.UserId)
               .IsRequired();
        builder.Property(x => x.BudgetId)
               .IsRequired();
        builder.Property(x => x.IsOwner)
               .IsRequired();
        builder.Property(BudgetAccess.PermissionsPropertyName)
               .IsRequired();

        builder.HasIndex(x => x.BudgetId)
               .IsUnique()
               .HasFilter("[IsOwner] = 1");

        builder.HasOne<ApplicationUser>()
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Budget)
               .WithMany(x => x.Accesses)
               .HasForeignKey(x => x.BudgetId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
