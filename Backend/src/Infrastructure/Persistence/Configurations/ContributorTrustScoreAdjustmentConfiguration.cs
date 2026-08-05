using Domain.Reputation;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ContributorTrustScoreAdjustmentConfiguration : IEntityTypeConfiguration<ContributorTrustScoreAdjustment>
{
    public void Configure(EntityTypeBuilder<ContributorTrustScoreAdjustment> builder)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.PerformedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
