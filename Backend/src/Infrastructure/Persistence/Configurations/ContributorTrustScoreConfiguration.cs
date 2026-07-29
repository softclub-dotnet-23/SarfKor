using Domain.Reputation;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ContributorTrustScoreConfiguration : IEntityTypeConfiguration<ContributorTrustScore>
{
    public void Configure(EntityTypeBuilder<ContributorTrustScore> builder)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
