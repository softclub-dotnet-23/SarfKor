using Domain.Offers;
using Domain.Products;
using Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ExpiringOfferConfiguration : IEntityTypeConfiguration<ExpiringOffer>
{
    public void Configure(EntityTypeBuilder<ExpiringOffer> builder)
    {
        builder.ComplexProperty(x => x.OriginalPrice, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.ComplexProperty(x => x.DiscountedPrice, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
