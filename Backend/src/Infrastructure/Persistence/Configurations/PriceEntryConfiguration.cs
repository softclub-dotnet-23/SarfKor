using Domain.Pricing;
using Domain.Products;
using Infrastructure.Identity;
using Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PriceEntryConfiguration : IEntityTypeConfiguration<PriceEntry>
{
    public void Configure(EntityTypeBuilder<PriceEntry> builder)
    {
        builder.ComplexProperty(x => x.Price, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
