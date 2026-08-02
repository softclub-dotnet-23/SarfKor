using Domain.Sales;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SaleLineItemConfiguration : IEntityTypeConfiguration<SaleLineItem>
{
    public void Configure(EntityTypeBuilder<SaleLineItem> builder)
    {
        builder.ComplexProperty(x => x.UnitPriceAtSale, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        // Restrict, not Cascade — matches every sibling financial config keyed off SaleTransaction
        // (Payment/Commission/SaleReturn/FiscalReceipt). A financial record shouldn't be able to
        // silently take its own line items with it if something ever deletes a SaleTransaction.
        builder.HasOne<SaleTransaction>()
            .WithMany(t => t.Lines)
            .HasForeignKey(x => x.SaleTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
