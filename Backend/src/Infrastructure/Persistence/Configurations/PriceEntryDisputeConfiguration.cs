using Domain.Pricing;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PriceEntryDisputeConfiguration : IEntityTypeConfiguration<PriceEntryDispute>
{
    public void Configure(EntityTypeBuilder<PriceEntryDispute> builder)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.DisputedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PriceEntry>()
            .WithMany()
            .HasForeignKey(x => x.PriceEntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
