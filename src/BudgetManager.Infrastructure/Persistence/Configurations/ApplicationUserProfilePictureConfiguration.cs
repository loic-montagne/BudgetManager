using BudgetManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetManager.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserProfilePictureConfiguration : IEntityTypeConfiguration<ApplicationUserProfilePicture>
{
    public void Configure(EntityTypeBuilder<ApplicationUserProfilePicture> builder)
    {
        builder.ConfigureAuditable();

        builder.HasKey(x => x.UserId);

        builder.Property(x => x.Content)
               .IsRequired();
        builder.Property(x => x.ContentType)
               .IsRequired()
               .UseCollation(ApplicationDbContext.StringComparisonCollation)
               .HasMaxLength(50);

        builder
            .HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<ApplicationUserProfilePicture>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
