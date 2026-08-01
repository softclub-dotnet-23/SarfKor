using Domain.Inventory;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PurchaseOrderLineItemConfiguration : IEntityTypeConfiguration<PurchaseOrderLineItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLineItem> builder)
    {
        builder.ComplexProperty(x => x.UnitCost, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        // Restrict, not Cascade — same reasoning as SaleLineItemConfiguration: a purchase order is
        // a financial/audit record, its lines shouldn't silently vanish if the order is ever deleted.
        builder.HasOne<PurchaseOrder>()
            .WithMany(o => o.Lines)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
