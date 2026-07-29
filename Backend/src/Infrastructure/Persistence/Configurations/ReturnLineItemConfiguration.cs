using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReturnLineItemConfiguration : IEntityTypeConfiguration<ReturnLineItem>
{
    public void Configure(EntityTypeBuilder<ReturnLineItem> builder)
    {
        builder.ComplexProperty(x => x.RefundAmount, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.HasOne<SaleReturn>()
            .WithMany(r => r.Lines)
            .HasForeignKey(x => x.SaleReturnId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<SaleLineItem>()
            .WithMany()
            .HasForeignKey(x => x.SaleLineItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
