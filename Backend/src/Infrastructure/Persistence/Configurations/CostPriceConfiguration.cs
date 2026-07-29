using Domain.Inventory;
using Domain.Products;
using Infrastructure.Identity;
using Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CostPriceConfiguration : IEntityTypeConfiguration<CostPrice>
{
    public void Configure(EntityTypeBuilder<CostPrice> builder)
    {
        builder.ComplexProperty(x => x.Amount, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.SetByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
