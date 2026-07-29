using Domain.Loyalty;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.HasOne<LoyaltyAccount>()
            .WithMany()
            .HasForeignKey(x => x.LoyaltyAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SaleTransaction>()
            .WithMany()
            .HasForeignKey(x => x.SaleTransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
