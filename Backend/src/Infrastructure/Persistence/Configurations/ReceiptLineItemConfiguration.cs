using Domain.Receipts;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReceiptLineItemConfiguration : IEntityTypeConfiguration<ReceiptLineItem>
{
    public void Configure(EntityTypeBuilder<ReceiptLineItem> builder)
    {
        builder.ComplexProperty(x => x.Price, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.HasOne<Receipt>()
            .WithMany(r => r.Lines)
            .HasForeignKey(x => x.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
