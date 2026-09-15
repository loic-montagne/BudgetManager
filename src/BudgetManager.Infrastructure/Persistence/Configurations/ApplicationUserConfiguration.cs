using BudgetManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ConfigureAuditable();

        builder.Property(x => x.UserName)
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(Domain.Common.StringPropertyLengths.EmailLength);
        builder.Property(x => x.Email)
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(Domain.Common.StringPropertyLengths.EmailLength);
        builder.Property(x => x.PhoneNumber)
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(Domain.Common.StringPropertyLengths.PhoneNumberLength);

        builder.Property(x => x.LastName)
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(Domain.Common.StringPropertyLengths.LastNameLength)
               .IsRequired();
        builder.Property(x => x.FirstName)
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(Domain.Common.StringPropertyLengths.FirstNameLength)
               .IsRequired();
        builder.Property(x => x.PreferredCulture)
               .HasMaxLength(10)
               .IsRequired();
        builder.Property(x => x.PreferredTheme)
               .HasMaxLength(10);

        builder.HasIndex(x => x.LastName);
        builder.HasIndex(x => new { x.LastName, x.FirstName });
        builder.HasIndex(x => new { x.FirstName, x.LastName });
    }
}
