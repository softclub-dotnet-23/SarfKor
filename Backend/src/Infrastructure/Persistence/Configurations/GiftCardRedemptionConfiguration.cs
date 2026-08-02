using Domain.Payments;
using Domain.Sales;
using Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class GiftCardRedemptionConfiguration : IEntityTypeConfiguration<GiftCardRedemption>
{
    public void Configure(EntityTypeBuilder<GiftCardRedemption> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasOne<GiftCard>()
            .WithMany()
            .HasForeignKey(x => x.GiftCardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SaleTransaction>()
            .WithMany()
            .HasForeignKey(x => x.SaleTransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
